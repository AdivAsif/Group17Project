namespace AuthenticationMicroservice.Models.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseObjects;

public class User : BaseIntEntity
{
    [Required] public string? FirstName { get; set; }

    [Required] public string? Surname { get; set; }

    [Required] public string? EmailAddress { get; set; }

    [Required] public string? Password { get; set; }

    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset? LastRefreshTokenIssued { get; set; }

    public string? UnconfirmedEmail { get; set; }

    public Guid? PasswordResetToken { get; set; }
    public Guid? EmailConfirmationToken { get; set; }

    [NotMapped] public string Username => FirstName + " " + Surname;
}