namespace Group17profile.Models.Entities;

using DefaultObjects;

public class Profile : DefaultGuidEntity
{
    public int? UserId { get; set; }
    public string? Biography { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string? BannerUrl { get; set; }

    public string? Gender { get; set; }

    public DateTimeOffset? DoB { get; set; }

    public string? FavouriteShows { get; set; }

    public string? Pronoun { get; set; }
}