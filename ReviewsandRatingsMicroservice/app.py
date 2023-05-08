# -*- coding: utf-8 -*-
# @Time : 2023/4/30 13:19 - 2023/5/06 16:17
# @Author : Jackson, Adiv
# @File : app.py
import json
import os.path
from functools import wraps

import jwt
import pandas as pd
from flask import Flask, request, jsonify
from flask_cors import CORS
from flask_restx import Api, Resource, fields
from pymongo import MongoClient, errors

app = Flask(__name__)
app.config.from_object(__name__)
CORS(app)
api = Api(app, version='1.0', title='Reviews and Ratings Microservice',
          description='This microservice returns reviews and ratings for TV Series',
          default="Reviews and Ratings", default_label="The Reviews and Ratings namespace")
api.documentation_path = "/swagger.json"


def _get_value_for_keys(keys, obj, default):
    if len(keys) == 1:
        return _get_value_for_key(keys[0], obj, default)
    else:
        return _get_value_for_keys(
            keys[1:], _get_value_for_key(keys[0], obj, default), default)


def _get_value_for_key(key, obj, default):
    if is_indexable_but_not_string(obj):
        try:
            return obj[key]
        except (IndexError, TypeError, KeyError):
            pass
    return getattr(obj, key, default)


def is_indexable_but_not_string(obj):
    return not hasattr(obj, "strip") and hasattr(obj, "__iter__")


def get_value(key, obj, default=None):
    '''Helper for pulling a keyed value off various types of objects'''
    if isinstance(key, int):
        return _get_value_for_key(key, obj, default)
    elif callable(key):
        return key(obj)
    else:
        return _get_value_for_keys(key.split('.'), obj, default)


class NullableFloat(fields.Float):
    __schema_type__ = ['number', 'null']
    __schema_format__ = 'float'
    __schema_example__ = 'nullable float'

    def output(self, key, obj, ordered=False):
        value = get_value(key, obj, ordered)
        if value is None or value == "":
            return None
        return float(value)


review_model = api.model('Review', {
    'TVSeriesId': fields.Integer(required=True, description='TV Series Id'),
    'UserId': fields.Integer(required=True, description='User Id'),
    'Review': fields.String(required=True, description='Review')
})

rating_model = api.model('Rating', {
    'TVSeriesId': fields.Integer(required=True, description='TV Series Id'),
    'UserId': fields.Integer(required=True, description='User Id'),
    'Rating': fields.Integer(required=True, description='Rating')
})

rating_with_tv_series_id_model = api.model('RatingWithTVSeries', {
    'UserId': fields.Integer(required=True, description='User Id'),
    'Rating': fields.Integer(required=True, description='Rating')
})

review_with_tv_series_id_model = api.model('ReviewWithTVSeries', {
    'UserId': fields.Integer(required=True, description='User Id'),
    'Review': fields.String(required=True, description='Review')
})

ratings_with_tv_series_id_model = api.model('RatingsWithTVSeries', {
    'TVSeriesId': fields.Integer(required=True, description='TV Series Id'),
    'Ratings': fields.List(fields.Nested(rating_with_tv_series_id_model))
})

reviews_with_tv_series_id_model = api.model('ReviewsWithTVSeries', {
    'TVSeriesId': fields.Integer(required=True, description='TV Series Id'),
    'Reviews': fields.List(fields.Nested(review_with_tv_series_id_model))
})

ratings_model = api.model('Ratings', {
    'TVSeriesRatings': fields.List(fields.Nested(rating_model))
})

reviews_model = api.model('Reviews', {
    'TVSeriesReviews': fields.List(fields.Nested(review_model))
})

average_ratings_model = api.model('AverageRating', {
    'AverageRating': fields.Integer(required=True, description='TV Series Average Ratings'),
    'TVSeriesId': fields.Integer(required=True, description='TV Series Id')
})


def initialise_db():
    try:
        if os.path.isfile('appsettings.json'):
            uri = pd.read_json('appsettings.json')['connectionString'].values[0]
            if uri:
                pass
            elif os.getenv('connectionString') is not None:
                uri = os.environ['connectionString']
            else:
                print('Connection string not provided')
                return None
        elif os.getenv('connectionString') is not None:
            uri = os.environ['connectionString']
        else:
            print('Connection string not provided')
            return None

        client = MongoClient(uri)
        client.server_info()
        db = client['db']
        ratings = db['ratings']
        reviews = db['reviews']
        return ratings, reviews
    except errors.ServerSelectionTimeoutError as err:
        print(f"***Error occurred connecting to MongoDB database***\n{err}")


def initialise_token_secret():
    if os.path.isfile('appsettings.json'):
        with open('appsettings.json') as f:
            config = json.load(f)
            secret = config.get('tokenSecret', {}).get('secret')
            if secret:
                return secret
            elif os.getenv('tokenSecret') is not None:
                secret = os.environ['tokenSecret']
                return secret
            else:
                print('Token settings not provided')
                return None
    elif os.getenv('tokenSecret') is not None:
        secret = os.environ['tokenSecret']
        return secret
    else:
        print('Token settings not provided')
        return None


TokenSecret = initialise_token_secret()


def bearer_token_required(f):
    @wraps(f)
    def decorated(*args, **kwargs):
        auth_header = request.headers.get('Authorization')
        if auth_header is None or not auth_header.startswith("Bearer "):
            return jsonify({"msg": "Missing or invalid Bearer Token"}), 401

        token = auth_header.split(" ")[1]
        try:
            jwt.decode(token, TokenSecret, algorithms=["HS256"])
        except jwt.InvalidTokenError:
            return jsonify({"msg": "Invalid Bearer Token"}), 401

        return f(*args, **kwargs)

    return decorated


# Create or update a rating record
@api.route('/api/rating', methods=['POST'], endpoint='create_or_update_rating')
@api.doc(body=rating_model, responses={200: 'application/json'})
class CreateOrUpdateRating(Resource):
    @api.marshal_with(rating_model)
    @bearer_token_required
    def post(self):
        data = request.get_json()
        tv_series_id = data['TVSeriesId']
        user_id = data['UserId']
        rating = data['Rating']
        # Check if the rating record already exists
        existing_rating = ratings.find_one({'TVSeriesId': tv_series_id, 'UserId': user_id})
        if existing_rating is None:
            # Create a new rating record
            new_rating = ratings.insert_one(
                {'TVSeriesId': tv_series_id, 'UserId': user_id, 'Rating': rating}).inserted_id
            return data
        else:
            # Update an existing rating record
            existing_rating = ratings.update_one({'_id': existing_rating['_id']}, {'$set': {'Rating': rating}})
        return data


# Create or update a review record
@api.route('/api/review', methods=['POST'], endpoint='create_or_update_review')
@api.doc(body=review_model, responses={200: 'application/json'})
class CreateOrUpdateReview(Resource):
    @api.marshal_with(review_model)
    @bearer_token_required
    def post(self):
        data = request.get_json()
        tv_series_id = data['TVSeriesId']
        user_id = data['UserId']
        review = data['Review']
        # Check if the review record already exists
        existing_review = reviews.find_one({'TVSeriesId': tv_series_id, 'UserId': user_id})
        if existing_review is None:
            # Create a new review record
            new_review = reviews.insert_one(
                {'TVSeriesId': tv_series_id, 'UserId': user_id, 'Review': review}).inserted_id
            return data
        else:
            # Update an existing review record
            existing_review = reviews.update_one({'_id': existing_review['_id']}, {'$set': {'Review': review}})
        return data


# Get all rating records for a TV series
@api.route('/api/ratings/all/<int:tv_series_id>', methods=['GET'], endpoint='get_all_ratings_by_tv_series_id')
@api.doc(params={'tv_series_id': 'A TV series'}, responses={200: 'application/json'})
class GetRatings(Resource):
    @api.marshal_with(ratings_with_tv_series_id_model)
    def get(self, tv_series_id):
        # Find all rating records for the TV series
        ratings_list = list(ratings.find({'TVSeriesId': tv_series_id}))
        # Create a dictionary to hold the TV series ID and ratings
        ratings_dict = {'TVSeriesId': tv_series_id, 'Ratings': []}
        # Add each rating to the list of ratings in the dictionary
        for rating in ratings_list:
            ratings_dict['Ratings'].append({'UserId': rating['UserId'], 'Rating': rating['Rating']})
        # Convert the ratings dictionary to JSON and return it
        return ratings_dict


# Get the average rating for a TV series
@api.route('/api/ratings/<int:tv_series_id>', methods=['GET'], endpoint='get_rating_by_tv_series_id')
@api.doc(params={'tv_series_id': 'A TV series'}, responses={200: 'application/json'})
class GetAverageRating(Resource):
    @api.marshal_with(average_ratings_model)
    def get(self, tv_series_id):
        # Find all rating records for the TV series
        ratings_list = list(ratings.find({'TVSeriesId': tv_series_id}))
        if len(ratings_list) > 0:
            # Convert all ratings to double type
            for rating in ratings_list:
                rating['Rating'] = float(rating['Rating'])
            # Calculate the average rating for the TV series
            ratings_sum = 0
            ratings_count = 0
            for rating in ratings_list:
                if rating['Rating'] is not None:
                    ratings_sum += rating['Rating']
                    ratings_count += 1
            average_rating = ratings_sum / ratings_count if ratings_count > 0 else 0
            average_rating = round(average_rating, 1)
            # Create a dictionary to hold the TV series ID and average rating
            rating_dict = {'TVSeriesId': tv_series_id, 'AverageRating': average_rating}
            # Convert the rating dictionary to JSON and return it
            return rating_dict
        else:
            # Return a default average rating of 0 if there are no ratings
            rating_dict = {'TVSeriesId': tv_series_id, 'AverageRating': 0}
            return rating_dict


# Get a review record
# Get all review records for a TV series
@api.route('/api/reviews/<int:tv_series_id>', methods=['GET'], endpoint='get_reviews_by_tv_series_id')
@api.doc(params={'tv_series_id': 'A TV series'}, responses={200: 'application/json'})
class GetReviews(Resource):
    @api.marshal_with(reviews_with_tv_series_id_model)
    def get(self, tv_series_id):
        # Find all review records for the TV series
        reviews_list = list(reviews.find({'TVSeriesId': tv_series_id}))
        # Create a dictionary to hold the TV series ID and reviews
        reviews_dict = {'TVSeriesId': tv_series_id, 'Reviews': []}
        # Add each review to the list of reviews in the dictionary
        for review in reviews_list:
            reviews_dict['Reviews'].append({'UserId': review['UserId'], 'Review': review['Review']})
        # Convert the reviews dictionary to JSON and return it
        return reviews_dict


def create_app():
    global ratings, reviews
    ratings, reviews = initialise_db()
    if ratings is None or reviews is None:
        print('Flask server not run as a connection to the MongoDB collection could not be established.')
        exit(1)

    return app


def app_with_args(environ, start_response):
    create_app()
    return app(environ, start_response)


if __name__ == '__main__':
    create_app()
    app.run(host='0.0.0.0', port=5001, debug=False)
