namespace Group17PortalWasm.Settings;

using Group17.Auth;
using Group17.Profile;
using Group17.ReviewsRatings;

public class ReviewModel
{
    public UserDTO? User { get; set; }
    public ProfileDTO? Profile { get; set; }
    public ReviewWithTVSeries? Review { get; set; }
}