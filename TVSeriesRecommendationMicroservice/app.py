import csv
import os
import re

import pandas as pd
from flask import Flask, jsonify
from flask_cors import CORS
from flask_restx import Api, Resource, fields
from pymongo import MongoClient, errors
from sklearn.feature_extraction.text import ENGLISH_STOP_WORDS as sklearn_stop_words
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from spacy.lang.en.stop_words import STOP_WORDS

# instantiate the app
app = Flask(__name__)
app.config.from_object(__name__)
# enable CORS
CORS(app)
# enable flask_restx
api = Api(app, version='1.0', title='TV Series Microservice',
          description='This microservice returns recommendations and a list of shows',
          default="TV Series", default_label="The TV Series namespace")
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


series_model = api.model('Series', {
    'id': fields.Integer(required=True, description='TV series id'),
    'name': fields.String(required=True, description='TV series name'),
})

series_name_model = api.model('SeriesName', {
    'names': fields.List(fields.Nested(series_model))
})

series_info_model = api.model('SeriesInfo', {
    'id': fields.Integer(required=True, description='TV series id'),
    'name': fields.String(required=True, description='TV series name'),
    'genre': fields.String(description='TV series genre'),
    'backdrop': fields.String(description='TV series backdrop'),
    'overview': fields.String(description='TV series overview'),
    'year_aired': NullableFloat(description='TV series year aired'),
    'poster': fields.String(description='TV series poster'),
    'cast': fields.String(description='TV series cast'),
    'feature': fields.String(description='TV series feature'),
    'rating': NullableFloat(required=False, description='TV series rating'),
    'keywords': fields.String(description='TV series keywords')
})

series_recommendation_model = api.model('SeriesRecommendation', {
    'series_info': fields.Nested(series_info_model)
})

series_recommendations_model = api.model('SeriesRecommendations', {
    'recommendations': fields.List(fields.Nested(series_info_model))
})

series_paged_model = api.model('SeriesPaged', {
    'page': fields.Integer,
    'shows': fields.List(fields.Nested(series_info_model)),
    'total_pages': fields.Integer,
    'total_shows': fields.Integer
})


def initialise_db():
    try:
        # check if appsettings.json exists
        if os.path.isfile('appsettings.json'):
            uri = pd.read_json('appsettings.json')['ConnectionString'].values[0]
            # check if connection string is empty
            if uri:
                pass
            # check if connection string is available as an environment variable
            elif os.getenv('ConnectionString') is not None:
                uri = os.environ['ConnectionString']
            else:
                print('Connection string not provided')
                return None
        elif os.getenv('ConnectionString') is not None:
            uri = os.environ['ConnectionString']
        else:
            print('Connection string not provided')
            return None

        # attempt to connect to MongoDB hosted on Azure
        mongoClient = MongoClient(uri)
        # verify successful connection by retrieving server info
        mongoClient.server_info()
        db = mongoClient['tv_db']
        collection = db['tv_shows']
        # insert csv file into a new db collection if the collection doesn't exist
        if collection.count_documents({}) == 0:
            df = pd.read_csv('tv_shows.csv')
            header = df.columns
            csvFile = open('tv_shows.csv', 'r')
            reader = csv.DictReader(csvFile)
            # write each row in the csv to a row in the collection
            for i in reader:
                row = {}
                for field in header:
                    row[field] = i[field]
                collection.insert_one(row)
        return collection
    # case when issue occurs connecting to MongoDB
    except errors.ServerSelectionTimeoutError as err:
        print(f"***Error occurred connecting to MongoDB database***\n{err}")


def create_similarity_matrix(collection):
    # import list of stopwords
    custom_stop_words = set(list(sklearn_stop_words) + list(STOP_WORDS))
    # initialise Tfidf Vectoriser to (include unigrams & bigrams and remove stopwords)
    tfidf = TfidfVectorizer(ngram_range=(1, 2), stop_words=list(custom_stop_words))
    feature_col = pd.Series([''.join(i.values()) for i in collection.find({}, {'_id': 0, 'feature': 1})])
    # apply Tfidf vectoriser to feature column of dataset
    tfidf_matrix = tfidf.fit_transform(feature_col)
    # get the cosine similarity of feature matrix
    return cosine_similarity(tfidf_matrix)


def recommend_tv_shows(tv_series):
    # search for tv series name (case-insensitive)
    name_query = {'name': re.compile(f'^{tv_series}$', re.IGNORECASE)}
    name_search = collection.find(name_query, {"_id": 0})
    result = [i for i in name_search]
    if len(result) > 0:
        result = result[0]
        collection_li = list(collection.find({}, {"_id": 0}))
        # get index location of selected tv series
        tv_series_idx = collection_li.index(result)
        # generate matrix of similarities
        similarities = create_similarity_matrix(collection)
        # select similarities pertaining to specified tv series
        tv_series_similarity = similarities[tv_series_idx]
        # number i.e. film idx, followed by similarity score
        tv_series_similarity_ranked = list(enumerate(tv_series_similarity))
        # sort in descending order & get the top 10 tv shows - excluding the first as it's the same tv show
        tv_series_similarity_ranked = sorted(tv_series_similarity_ranked, key=lambda x: x[1], reverse=True)[1:11]
        # convert 2D array to list of names of tv series
        tv_series_names = [collection_li[i[0]] for i in tv_series_similarity_ranked]
    else:
        tv_series_names = []
    return tv_series_names


def recommend_tv_shows_by_id(tv_series_id):
    # search for tv series name (case-insensitive)
    id_query = {'id': re.compile(f'^{tv_series_id}$', re.IGNORECASE)}
    id_search = collection.find(id_query, {"_id": 0})
    result = [i for i in id_search]
    if len(result) > 0:
        result = result[0]
        collection_li = list(collection.find({}, {"_id": 0}))
        # get index location of selected tv series
        tv_series_idx = collection_li.index(result)
        # generate matrix of similarities
        similarities = create_similarity_matrix(collection)
        # select similarities pertaining to specified tv series
        tv_series_similarity = similarities[tv_series_idx]
        # number i.e. film idx, followed by similarity score
        tv_series_similarity_ranked = list(enumerate(tv_series_similarity))
        # sort in descending order & get the top 10 tv shows - excluding the first as it's the same tv show
        tv_series_similarity_ranked = sorted(tv_series_similarity_ranked, key=lambda x: x[1], reverse=True)[1:11]
        # convert 2D array to list of names of tv series
        tv_series_names = [collection_li[i[0]] for i in tv_series_similarity_ranked]
    else:
        tv_series_names = []
    return tv_series_names


@api.route('/api/recommendations/<string:tv_series>')
@api.doc(params={'tv_series': 'A TV series'}, responses={200: 'application/json'})
class Recommendations(Resource):
    @api.marshal_with(series_recommendations_model)
    def get(self, tv_series):
        # get list of recommendations
        recommendations = recommend_tv_shows(tv_series)
        # convert list to dictionary
        api_result = {'recommendations': recommendations}
        return api_result


@api.route('/api/recommendationsById/<int:tv_series>')
@api.doc(params={'tv_series': 'A TV series'}, responses={200: 'application/json'})
class RecommendationsById(Resource):
    @api.marshal_with(series_recommendations_model)
    def get(self, tv_series):
        # get list of recommendations
        recommendations = recommend_tv_shows_by_id(tv_series)
        # convert list to dictionary
        api_result = {'recommendations': recommendations}
        return api_result


@api.route('/api/shows/<string:tv_series>')
@api.doc(responses={200: 'application/json'})
class SeriesNames(Resource):
    @api.marshal_with(series_name_model)
    def get(self, tv_series):
        # get entire collection as a list
        collection_li = list(collection.find({}, {"_id": 0, "id": 1, "name": 1}))
        # select only the names of the tv series
        if len(tv_series) < 3:
            return jsonify({'names': []})
        else:
            # series_names = [i['name'] for i in collection_li if tv_series.lower() in i['name'].lower()]
            series_names = [{"id": i.get("id", ""), "name": i.get("name", "")} for i in collection_li if
                            tv_series.lower() in i.get("name", "").lower()]
            # return as a dict
            result = {'names': series_names}
            return result


@api.route('/api/info/<string:tv_series>')
@api.doc(responses={200: 'application/json'})
class SeriesInfo(Resource):
    @api.marshal_with(series_recommendation_model)
    def get(self, tv_series):
        # search for tv series name (case-insensitive)
        name_query = {'name': re.compile(f'^{tv_series}$', re.IGNORECASE)}
        name_search = collection.find(name_query, {"_id": 0})
        series_info = [i for i in name_search][0]
        # return series info as a dict
        result = {'series_info': series_info}
        return result


@api.route('/api/infoById/<int:tv_series>')
@api.doc(responses={200: 'application/json'})
class SeriesInfoById(Resource):
    @api.marshal_with(series_recommendation_model)
    def get(self, tv_series):
        # search for tv series id
        id_query = {'id': re.compile(f'^{tv_series}$')}
        id_search = collection.find(id_query, {"_id": 0})
        series_info = [i for i in id_search][0]
        # return series info as a dict
        result = {'series_info': series_info}
        return result


@api.route('/api/tv_shows/<int:page>')
@api.doc(responses={200: 'application/json'})
class TvShows(Resource):
    @api.marshal_with(series_paged_model)
    def get(self, page):
        # Get the page number from the request arguments, defaulting to 1 if not provided
        # page = int(request.args.get('page', 1))

        # Define the number of shows to display per page
        shows_per_page = 20

        # Calculate the starting and ending indices for the page
        start_idx = (page - 1) * shows_per_page
        end_idx = start_idx + shows_per_page

        # Get the shows for the requested page
        collection_li = list(collection.find({}, {"_id": 0, "backdrop": 1, "cast": 1, "feature": 1, "genre": 1, "id": 1,
                                                  "keywords": 1, "name": 1, "overview": 1, "poster": 1, "rating": 1,
                                                  "year_aired": 1}))
        shows = collection_li[start_idx:end_idx]
        # Create a list of dictionaries containing the relevant information for each show
        response_data = []
        for show in shows:
            show_data = {
                'id': show['id'],
                'backdrop': show['backdrop'],
                'cast': show['cast'],
                'feature': show['feature'],
                'genre': show['genre'],
                'id': show['id'],
                'keywords': show['keywords'],
                'name': show['name'],
                'overview': show['overview'],
                'poster': show['poster'],
                'rating': show['rating'],
                'year_aired': show['year_aired']
            }
            response_data.append(show_data)

        # Create a response object with the page of shows and the relevant metadata
        response = {
            'page': page,
            'total_pages': (len(collection_li) + shows_per_page - 1) // shows_per_page,
            'total_shows': len(collection_li),
            'shows': response_data
        }

        return response


def create_app():
    global collection
    collection = initialise_db()
    if collection is None:
        print('Flask server not run as a connection to the MongoDB collection could not be established.')
        exit(1)

    return app


def app_with_args(environ, start_response):
    create_app()
    return app(environ, start_response)


if __name__ == '__main__':
    create_app()
    app.run(host='0.0.0.0', port=5000, debug=False)
