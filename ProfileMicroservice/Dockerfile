FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.3 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080
EXPOSE 5003
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.3 AS build
WORKDIR /src
COPY ["Group17profile.csproj", "./"]
RUN dotnet restore "Group17profile.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Group17profile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Group17profile.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Group17profile.dll"]
