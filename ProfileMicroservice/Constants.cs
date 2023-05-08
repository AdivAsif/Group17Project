namespace Group17profile;

public class Constants
{
    public enum Roles
    {
        Api = 0,
        Server = 1,
        Administrator = 2
    }

    public static class Claims
    {
        public const string Username = "username";
        public const string UserSignUpDate = "userSignUpDate";

        public const string AccessToken = "accessToken";
        public const string RefreshToken = "refreshToken";
        public const string ProfileSasToken = "profileSasToken";
    }

    public static class AzureBlobContainer
    {
        public const string ProfilePictures = "profile-pictures";
        public const string BannerPictures = "banner-pictures";
    }

    public static class AuthorizationPolicies
    {
        public const string HasUserId = "HasUserId";
    }

    public static class AuthSchemes
    {
        public const string ApiKeyScheme = "api-key-auth";
        public const string ServerKeyScheme = "server-key-auth";
    }

    public static class Tokens
    {
        public const string AccessToken = "accessToken";
        public const string RefreshToken = "refreshToken";
        public const string ProfileSasToken = "profileSasToken";
    }

    public static class HeaderKeys
    {
        public const string UserId = "user-id";
    }
}