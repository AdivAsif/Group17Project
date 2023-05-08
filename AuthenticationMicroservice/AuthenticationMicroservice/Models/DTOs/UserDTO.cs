namespace AuthenticationMicroservice.Models.DTOs;

using System.ComponentModel.DataAnnotations;
using BaseObjects;

public class UserDTO : BaseIntDto
{
    [Required] public string? FirstName { get; set; }

    [Required] public string? Surname { get; set; }

    public string Username => $"{FirstName} {Surname}";

    [Required] public string? EmailAddress { get; set; }

    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset? LastRefreshTokenIssued { get; set; }

    public string? UnconfirmedEmail { get; set; }
}

public class UserLoginRequestDTO
{
    public string? EmailAddress { get; set; }
    public string? Password { get; set; }
}

public class UserRegisterRequestDTO : UserDTO
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? ConfirmPassword { get; set; }

    public string? CallbackUrl { get; set; }
}

public abstract class ChangeEmailRequestDTO
{
    [Required] public string? NewEmail { get; set; }

    [Required] public string? ConfirmEmail { get; set; }

    [Required] public string? CallbackUrl { get; set; }
}

public abstract class ForgotPasswordRequestDTO
{
    [Required] public string? EmailAddress { get; set; }

    [Required] public string? CallbackUrl { get; set; }
}

public abstract class SetPasswordRequestDTO
{
    [Required] public string? Token { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? ConfirmPassword { get; set; }
}

public abstract class ChangePasswordRequestDTO
{
    [Required] public string? OldPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string? ConfirmPassword { get; set; }
}

public class RefreshTokenRequestDTO
{
    public string? RefreshToken { get; set; }
}

public class AuthenticatedUserDTO
{
    public int UserId { get; set; }

    public string? AccessToken { get; set; }

    public long AccessTokenExpiresIn { get; set; }

    public string? RefreshToken { get; set; }

    public long RefreshTokenExpires { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? ProfilePictureSas { get; set; }
}