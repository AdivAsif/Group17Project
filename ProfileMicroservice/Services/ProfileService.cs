namespace Group17profile.Services;

using System.Text;
using AutoMapper;
using Exceptions;
using Models.DTOs;
using Repositories;
using Profile = Models.Entities.Profile;

public interface IProfileService
{
    Task<ProfileDTO> GetProfileForUser(int userId);
    Task<List<ProfileDTO>> GetProfilesByUserIds(List<int> userIds);
    Task<ProfileDTO> CreateOrUpdateProfile(ProfileDTO profile, int userId);
    Task<ProfileDTO> UploadProfilePicture(int userId, IFormFile profilePicture);
    Task<ProfileDTO> UploadBannerPicture(int userId, IFormFile bannerPicture);
}

public class ProfileService : IProfileService
{
    private readonly IMapper _mapper;
    private readonly IProfileRepository _profileRepository;
    private readonly IStorageService _storageService;

    public ProfileService(IMapper mapper, IProfileRepository profileRepository, IStorageService storageService)
    {
        _mapper = mapper;
        _profileRepository = profileRepository;
        _storageService = storageService;
    }

    public async Task<ProfileDTO> GetProfileForUser(int userId)
    {
        var profile = await _profileRepository.GetProfileAsync(userId);
        if (profile == null)
            throw new ProfileException("Profile does not exist.");
        if (!string.IsNullOrWhiteSpace(profile.ProfilePictureUrl))
            profile.ProfilePictureUrl +=
                _storageService.GetSasForFile(Constants.AzureBlobContainer.ProfilePictures, profile.ProfilePictureUrl);
        if (!string.IsNullOrWhiteSpace(profile.BannerUrl))
            profile.BannerUrl +=
                _storageService.GetSasForFile(Constants.AzureBlobContainer.BannerPictures, profile.BannerUrl);
        var record = _mapper.Map<ProfileDTO>(profile);
        var s = profile.FavouriteShows?.Split(",");
        record.FavouriteShows = s?.ToList();

        return record;
    }

    public async Task<ProfileDTO> CreateOrUpdateProfile(ProfileDTO profile, int userId)
    {
        var age = DateTimeOffset.Now.Year - profile.DoB.GetValueOrDefault().Year;
        if (age is <= 16 or >= 100)
            throw new ProfileException("Please enter valid age.");
        if (!string.IsNullOrWhiteSpace(profile.ProfilePictureUrl))
        {
            var profilePictureUri = new Uri(profile.ProfilePictureUrl);
            if (!string.IsNullOrWhiteSpace(profilePictureUri.Query))
                profile.ProfilePictureUrl = profile.ProfilePictureUrl.Replace(profilePictureUri.Query, "");
        }

        if (!string.IsNullOrWhiteSpace(profile.BannerUrl))
        {
            var bannerUri = new Uri(profile.BannerUrl);
            if (!string.IsNullOrWhiteSpace(bannerUri.Query))
                profile.BannerUrl = profile.BannerUrl.Replace(bannerUri.Query, "");
        }

        var record = _mapper.Map<Profile>(profile);
        if (profile.FavouriteShows != null && profile.FavouriteShows.Count != 0)
        {
            var builder = new StringBuilder();
            foreach (var show in profile.FavouriteShows)
                builder.Append($"{show},");

            record.FavouriteShows = builder.ToString();
        }

        var existing = await _profileRepository.GetProfileAsync(userId);
        if (existing == null)
        {
            record.UserId = userId;
            record = await _profileRepository.CreateProfileAsync(record);
        }
        else
        {
            record.Id = existing.Id;
            record.UserId = existing.UserId;
            record = await _profileRepository.UpdateProfileAsync(userId, record.Id.ToString(), record);
        }

        if (!string.IsNullOrWhiteSpace(record.ProfilePictureUrl))
            record.ProfilePictureUrl +=
                _storageService.GetSasForFile(Constants.AzureBlobContainer.ProfilePictures, record.ProfilePictureUrl);

        if (!string.IsNullOrWhiteSpace(record.BannerUrl))
            record.BannerUrl +=
                _storageService.GetSasForFile(Constants.AzureBlobContainer.BannerPictures, record.BannerUrl);

        return _mapper.Map<ProfileDTO>(record);
    }

    public async Task<ProfileDTO> UploadProfilePicture(int userId, IFormFile profilePicture)
    {
        var profile = await _profileRepository.GetProfileAsync(userId);
        using var ms = new MemoryStream();
        await profilePicture.CopyToAsync(ms);
        ms.Position = 0;
        var fileName = $"{userId}/{profilePicture.Name}";
        var image = await _storageService.SaveImageAsJpgBlob(Constants.AzureBlobContainer.ProfilePictures, fileName,
            ms);
        if (profile == null)
        {
            profile = new Profile {ProfilePictureUrl = image.ToString(), Id = Guid.NewGuid(), UserId = userId};
            profile = await _profileRepository.CreateProfileAsync(profile);
        }
        else
        {
            profile.ProfilePictureUrl = image.ToString();
            profile = await _profileRepository.UpdateProfileAsync(userId, profile.Id.ToString(), profile);
        }

        profile.ProfilePictureUrl +=
            _storageService.GetSasForFile(Constants.AzureBlobContainer.ProfilePictures, image.ToString());
        return _mapper.Map<ProfileDTO>(profile);
    }

    public async Task<ProfileDTO> UploadBannerPicture(int userId, IFormFile bannerPicture)
    {
        var profile = await _profileRepository.GetProfileAsync(userId);
        using var ms = new MemoryStream();
        await bannerPicture.CopyToAsync(ms);
        ms.Position = 0;
        var fileName = $"{userId}/{bannerPicture.Name}";
        var image = await _storageService.SaveImageAsJpgBlob(Constants.AzureBlobContainer.BannerPictures, fileName, ms);
        if (profile == null)
        {
            profile = new Profile {BannerUrl = image.ToString(), Id = Guid.NewGuid(), UserId = userId};
            profile = await _profileRepository.CreateProfileAsync(profile);
        }
        else
        {
            profile.BannerUrl = image.ToString();
            profile = await _profileRepository.UpdateProfileAsync(userId, profile.Id.ToString(), profile);
        }

        profile.BannerUrl +=
            _storageService.GetSasForFile(Constants.AzureBlobContainer.BannerPictures, image.ToString());
        return _mapper.Map<ProfileDTO>(profile);
    }

    public async Task<List<ProfileDTO>> GetProfilesByUserIds(List<int> userIds)
    {
        var profiles = await _profileRepository.GetProfilesByUserIds(userIds);
        var mapped = _mapper.ProjectTo<ProfileDTO>(profiles.AsQueryable()).ToList();
        if (profiles.Count == 0) return mapped;
        foreach (var profile in mapped)
        {
            if (!string.IsNullOrWhiteSpace(profile.ProfilePictureUrl))
                profile.ProfilePictureUrl +=
                    _storageService.GetSasForFile(Constants.AzureBlobContainer.ProfilePictures,
                        profile.ProfilePictureUrl);
            if (!string.IsNullOrWhiteSpace(profile.BannerUrl))
                profile.BannerUrl +=
                    _storageService.GetSasForFile(Constants.AzureBlobContainer.BannerPictures, profile.BannerUrl);
        }

        return mapped;
    }
}