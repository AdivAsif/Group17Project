namespace Group17PortalWasm.Services;

using Group17.Profile;

public class ProfileStateService
{
    private readonly IProfileClient _profileClient;
    private ProfileInfo? _profileInfo;

    public ProfileStateService(IProfileClient profileClient)
    {
        _profileClient = profileClient;
    }

    public event Action? OnChange;

    public async Task<ProfileInfo?> GetProfileInfo()
    {
        if (_profileInfo != null) return _profileInfo;
        var profileFromApi = await _profileClient.GetProfileForUserAsync();
        _profileInfo = new ProfileInfo {ProfileDetails = profileFromApi.Data};

        return _profileInfo;
    }

    public async Task UpdateProfileInfoAsync(ProfileInfo profileInfo)
    {
        _profileInfo = profileInfo;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}