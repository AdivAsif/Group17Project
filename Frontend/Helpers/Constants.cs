namespace Group17PortalWasm.Helpers;

public static class Constants
{
    public enum Roles
    {
        Api = 0,
        Server = 1,
        Administrator = 2,
        Therapist = 3,
        Chat = 4,
        Voice = 5,
        Video = 6,
        News = 7
    }

    public static Dictionary<string, string> Moods = new()
    {
        {"Happy", "Sad"},
        {"Excited", "Bored"},
        {"Confident", "Insecure"},
        {"Relaxed", "Stressed"},
        {"Peaceful", "Angry"},
        {"Prideful", "Shameful"}
    };

    public static class HttpClientNames
    {
        public const string ApiCache = "portal-cached-information";
        public const string ApiClient = "api-client";
        public const string AuthClient = "auth-client";
        public const string ProfileClient = "profile-client";
    }

    public static class Claims
    {
        public const string Firstname = "firstname";
        public const string Surname = "surname";
        public const string Username = "username";
        public const string UserSignUpDate = "userSignUpDate";

        public const string AccessToken = "accessToken";
        public const string RefreshToken = "refreshToken";
    }
}