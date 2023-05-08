namespace Group17profile.Models.DTOs;

using DefaultObjects;

public class ProfileDTO : DefaultGuidDTO
{
    public int? UserId { get; set; }
    public string? Biography { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string? BannerUrl { get; set; }

    public string? Gender { get; set; }

    public DateTimeOffset? DoB { get; set; }

    public List<string>? FavouriteShows { get; set; }

    public string? Pronoun { get; set; }
}