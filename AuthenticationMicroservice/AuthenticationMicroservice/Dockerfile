FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.3 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 5001
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.3 AS build
WORKDIR /src
COPY ["AuthenticationMicroservice.csproj", "."]
RUN dotnet restore "./AuthenticationMicroservice.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AuthenticationMicroservice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthenticationMicroservice.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthenticationMicroservice.dll"]