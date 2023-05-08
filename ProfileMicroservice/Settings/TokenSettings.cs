namespace Group17profile.Settings;

public class TokenSettings
{
    public int RefreshTokenValidityMinutes { get; set; } = 43200;
    public int AccessTokenValidityMinutes { get; set; } = 60;
    public int RefreshTokenRandomNumbers { get; set; } = 32;
    public string Secret { get; set; } = string.Empty;
    public bool AllowMultipleRefreshTokens { get; set; } = true;
}