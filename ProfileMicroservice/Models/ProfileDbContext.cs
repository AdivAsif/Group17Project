namespace Group17profile.Models;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Settings;

public class ProfileDbContext
{
    private readonly Container _container;

    public ProfileDbContext(IOptions<ConnectionStrings> connectionStrings)
    {
        var clientOptions = new CosmosClientOptions
        {
            RequestTimeout = TimeSpan.FromSeconds(30)
        };
        var connectionString = connectionStrings.Value.DbConnectionString ?? "DbConnectionString";
        var client = new CosmosClient(connectionString, clientOptions);
        var database = client.GetDatabase("ProfileDb");
        _container = database.GetContainer("Profile");
    }

    public Container? Profile => _container;
}