# Microservices

This will be a guide on how to run each Microservice locally, and on a Docker container

To run the Authentication and Profile microservices locally, .NET 8 preview 3 will need to be installed from here: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

# Authentication Microservice

If .NET 8 is installed, run the following commands from the root of AuthenticationMicroservice:
``` bash
cd .\AuthenticationMicroservice\
dotnet build
dotnet run --launch-profile https
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
https://localhost:7224/swagger/v1/swagger.json
```
This will however require values to be filled within the appsettings.json, for it to be able to connect to a database and such.

To run the Docker container for the Authentication Microservice, in the root of AuthenticationMicroservice run the following commands (environment variables will have to be passed in for it to function correctly, which will not be provided):

``` bash
cd .\AuthenticationMicroservice\
docker build -t group17-auth-microservice .
docker run -p 8080:8080 -e ConnectionStrings__DbConnectionString="" -e TokenSettings__Secret="" -e EmailConfig__Email="" -e FrontendStrings__BaseUrl="" group17-auth-microservice
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
http:localhost:8080/swagger/v1/swagger.json
```
Also note, that if an error such as a Connection Timeout Expired arises, this may be due to the database still booting up - due to the Auto-Pause delay of an hour.

To run the unit tests, in the root of the AuthenticationMicroservice run the following commands:
``` bash
cd .\AuthenticationMicroservice.Tests\
dotnet test
```

# Profile Microservice

If .NET 8 is installed, run the following commands from the root of ProfileMicroservice:
``` bash
dotnet build
dotnet run --launch-profile https
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
https://localhost:7244/swagger/v1/swagger.json
```
This will however require values to be filled within the appsettings.json, for it to be able to connect to a database and such.

To run the Docker container for the Profile Microservice, in the root of ProfileMicroservice run the following commands (environment variables will have to be passed in for it to function correctly, which will not be provided):

``` bash
docker build -t group17-profile-microservice .
docker run -p 8080:8080 -e ConnectionStrings__DbConnectionString="" -e ConnectionStrings__AzureBlobStorage="DefaultEndpointsProtocol" -e TokenSettings__Secret="" -e FrontendStrings__BaseUrl="" group17-profile-microservice
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
http:localhost:8080/swagger/v1/swagger.json
```

# TV Series Recommendation Microservice

If the Python packages inside of requirements.txt on your machine are installed, run the following commands from the root of TVSeriesRecommendationMicroservice (if appsettings.json is not provided, it will use the tv_shows.csv file instead):
``` bash
python app.py
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
http://131.227.230.109:5000/swagger.json
```

To run the Docker container for the TV Series Recommendation Microservice, in the root of TVSeriesRecommendationMicroservice run the following commands (environment variables will have to be passed in for it to function correctly, which will not be provided):

``` bash
docker build -t group17-recommendation-microservice .
docker run -p 5000:5000 -e ConnectionString="" group17-recommendation-microservice
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON from before

# Reviews and Ratings Microservice

If the Python packages inside of requirements.txt on your machine are installed, run the following commands from the root of ReviewsandRatingsMicroservice (if appsettings.json is not provided, it will fail to connect to a database):
``` bash
python app.py
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON at:
```
http://131.227.230.109:5001/swagger.json
```

To run the Docker container for the Reviews and Ratings Microservice, in the root of ReviewsandRatingsMicroservice run the following commands (environment variables will have to be passed in for it to function correctly, which will not be provided):

``` bash
docker build -t group17-reviews-and-ratings-microservice .
docker run -p 5001:5001 -e connectionString="" -e tokenSecret="" group17-reviews-and-ratings-microservice
```

Requests can be made using something like Postman to see what gets returned when certain endpoints are hit. The endpoints can be found in the Swagger generated JSON from before

# Frontend

This will be a guide on how to run the frontend locally

To run the frontend locally, .NET 7.0.5 will need to be installed from here: https://dotnet.microsoft.com/en-us/download/dotnet/7.0

If .NET 7.0.5 is installed, run the following commands from the root of Frontend:
``` bash
dotnet build
dotnet run --launch-profile https
```

You can access the frontend at:
``` bash
https://localhost:7185/
```

If appsettings.json is present, it will use the microservices (Container Apps) hosted in Azure, if you would like to use it with local or Docker versions of the microservice, change the strings inside of appsettings.json
