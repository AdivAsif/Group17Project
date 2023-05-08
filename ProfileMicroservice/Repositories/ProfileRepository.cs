namespace Group17profile.Repositories;

using System.Net;
using Microsoft.Azure.Cosmos;
using Models;
using Models.Entities;

public interface IProfileRepository
{
    Task<Profile> CreateProfileAsync(Profile profile);
    Task<Profile?> GetProfileAsync(int userId);
    Task<List<Profile>> GetProfilesByUserIds(List<int> userIds);
    Task<Profile> UpdateProfileAsync(int userId, string profileId, Profile profile);
    Task DeleteProfileAsync(int userId, string profileId);
}

public class ProfileRepository : IProfileRepository
{
    private readonly Container? _profileContainer;

    public ProfileRepository(ProfileDbContext context)
    {
        _profileContainer = context.Profile;
    }

    public async Task<Profile> CreateProfileAsync(Profile profile)
    {
        var response =
            await _profileContainer?.CreateItemAsync(profile,
                new PartitionKey(profile.UserId.GetValueOrDefault()))!;
        return response.Resource;
    }

    public async Task<Profile?> GetProfileAsync(int userId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.UserId = @UserId")
                .WithParameter("@UserId", userId);

            var iterator = _profileContainer?.GetItemQueryIterator<Profile>(query);

            if (iterator is {HasMoreResults: false}) return null;
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<Profile>> GetProfilesByUserIds(List<int> userIds)
    {
        try
        {
            var userIdsString = string.Join(",", userIds);
            var queryString = $"SELECT * FROM c WHERE c.UserId IN ({userIdsString})";
            var query = new QueryDefinition(queryString);

            var iterator = _profileContainer?.GetItemQueryIterator<Profile>(query);

            if (iterator is {HasMoreResults: false}) return new List<Profile>();
            var profiles = new List<Profile>();

            while (iterator is {HasMoreResults: true})
            {
                var response = await iterator.ReadNextAsync();
                profiles.AddRange(response);
            }

            return profiles;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new List<Profile>();
        }
    }

    public async Task<Profile> UpdateProfileAsync(int userId, string profileId, Profile profile)
    {
        var response = await _profileContainer.ReplaceItemAsync(profile, profileId, new PartitionKey(userId));
        return response.Resource;
    }

    public async Task DeleteProfileAsync(int userId, string profileId)
    {
        await _profileContainer.DeleteItemAsync<Profile>(profileId, new PartitionKey(userId));
    }
}