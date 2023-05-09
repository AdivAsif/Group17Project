namespace AuthenticationMicroservice.Services;

using System.Net;
using System.Text;
using Exceptions;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.DTOs;
using Models.Entities;
using Settings;
using static Guid;

public interface IAuthService
{
    Task<AuthenticatedUserDTO> Authenticate(UserLoginRequestDTO userLogin);
    Task<AuthenticatedUserDTO> RefreshToken(string oldAccessToken, string? oldRefreshToken);
    Task<AuthenticatedUserDTO> GenerateAuthenticatedUser(User user, string? oldToken = null);
    Task ForgotPassword(ForgotPasswordRequestDTO request);
    Task SetPassword(SetPasswordRequestDTO request);
    Task ChangePassword(int userId, ChangePasswordRequestDTO request);
    Task ChangeEmail(int userId, ChangeEmailRequestDTO request);
    Task ResendConfirmEmail(int userId);
    Task ConfirmEmailAddress(int userId, string token);
    Task Logout(int userId);
}

public class AuthService : IAuthService
{
    private readonly AuthenticationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly FrontendStrings _frontendStrings;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(AuthenticationDbContext context, IEmailService emailService,
        IRefreshTokenService refreshTokenService, IOptions<FrontendStrings> frontendStrings)
    {
        _context = context;
        _emailService = emailService;
        _frontendStrings = frontendStrings.Value;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticatedUserDTO> Authenticate(UserLoginRequestDTO userLogin)
    {
        if (string.IsNullOrWhiteSpace(userLogin.EmailAddress))
            throw new AuthenticationException("Please enter a valid email.", HttpStatusCode.Unauthorized);
        if (string.IsNullOrWhiteSpace(userLogin.Password))
            throw new AuthenticationException("Please enter a password.", HttpStatusCode.Unauthorized);

        var user = await _context.User.FirstOrDefaultAsync(u =>
            string.Equals(u.EmailAddress, userLogin.EmailAddress));
        if (user == null)
            throw new AuthenticationException($"User with email: {userLogin.EmailAddress} does not exist.");
        if (!ValidatePassword(userLogin, user))
            throw new AuthenticationException("Incorrect email address or password.", HttpStatusCode.Unauthorized);

        return await GenerateAuthenticatedUser(user);
    }

    public async Task<AuthenticatedUserDTO> RefreshToken(string oldAccessToken, string? oldRefreshToken)
    {
        var userId = _refreshTokenService.GetUserIdFromExpiredToken(oldAccessToken);
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User not found.", HttpStatusCode.Unauthorized);
        return await GenerateAuthenticatedUser(user, oldRefreshToken);
    }

    public async Task<AuthenticatedUserDTO> GenerateAuthenticatedUser(User user, string? oldToken = null)
    {
        var newAccess = _refreshTokenService.CreateNewTokenForUser(user);
        var newRefresh = string.IsNullOrWhiteSpace(oldToken)
            ? await _refreshTokenService.GenerateRefreshToken(user.Id)
            : await _refreshTokenService.RefreshToken(oldToken, user.Id);

        var authenticatedUser = new AuthenticatedUserDTO
        {
            AccessToken = newAccess.Token,
            AccessTokenExpiresIn = newAccess.ExpiresIn,
            RefreshToken = newRefresh!.Token,
            RefreshTokenExpires = newRefresh.Expires,
            UserId = user.Id
        };

        await UpdateLastLoginForUser(user, !string.IsNullOrWhiteSpace(oldToken));
        return authenticatedUser;
    }

    public Task Logout(int userId)
    {
        return _refreshTokenService.InvalidateTokens(userId, $"Logout requested {DateTimeOffset.Now}.");
    }

    public async Task ChangeEmail(int userId, ChangeEmailRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.NewEmail) || string.IsNullOrWhiteSpace(request.ConfirmEmail))
            throw new AuthenticationException("Please enter emails in both fields.");
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User not found.", HttpStatusCode.Unauthorized);
        if (user.EmailAddress == user.UnconfirmedEmail)
        {
            user.EmailAddress = request.NewEmail.ToLower();
            user.UnconfirmedEmail = request.NewEmail.ToLower();
        }
        else
        {
            user.UnconfirmedEmail = request.NewEmail.ToLower();
        }

        user.EmailConfirmationToken = NewGuid();
        _context.User.Update(user);

        await _context.SaveChangesAsync();
        _emailService.SendEmail(new EmailDTO
                {Subject = "Email Changed", Body = "Test", To = request.NewEmail, Receiver = user.Username})
            .FireAndForget();
    }

    public async Task ConfirmEmailAddress(int userId, string token)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User not found.", HttpStatusCode.Unauthorized);
        if (string.IsNullOrWhiteSpace(user.UnconfirmedEmail))
            throw new AuthenticationException("Email already confirmed.", HttpStatusCode.BadRequest);
        var tryParse = TryParse(token[1..], out var parsedToken);
        if (!tryParse)
            throw new AuthenticationException("Token in request is not valid.");
        if (user.EmailConfirmationToken != parsedToken)
            throw new AuthenticationException("Token expired.");
        user.EmailAddress = user.UnconfirmedEmail.ToLower();
        user.EmailConfirmationToken = null;
        user.UnconfirmedEmail = null;
        _context.User.Update(user);

        await _context.SaveChangesAsync();
    }

    public async Task ResendConfirmEmail(int userId)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.EmailConfirmationToken == null)
            throw new AuthenticationException("User already confirmed.", HttpStatusCode.Unauthorized);
        _emailService.SendEmail(new EmailDTO
            {
                Subject = "Resend Confirm Email", Body = GetResendConfirmTableEmail(user), To = user.UnconfirmedEmail!,
                Receiver = user.Username
            })
            .FireAndForget();
    }

    public async Task ForgotPassword(ForgotPasswordRequestDTO request)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress);
        if (user == null)
            throw new AuthenticationException($"User with email: {request.EmailAddress} does not exist.",
                HttpStatusCode.Unauthorized);
        user.PasswordResetToken = NewGuid();
        _context.User.Update(user);
        await _context.SaveChangesAsync();
        _emailService.SendEmail(new EmailDTO
            {
                Subject = "Forgot Password", Body = GetForgotPasswordTableEmail(user), To = request.EmailAddress!,
                Receiver = user.Username
            })
            .FireAndForget();
    }

    public async Task SetPassword(SetPasswordRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new SecurityHelper.InvalidPasswordException("Please enter a password.");
        var tryParse = TryParse(request.Token?[1..], out var token);
        if (!tryParse)
            throw new AuthenticationException("Token in request is not valid.");
        var user = _context.User.FirstOrDefault(u => u.PasswordResetToken == token);
        if (user == null)
            throw new SecurityTokenException("Invalid password reset token.");
        var requestedTime = user.UpdatedAt.GetValueOrDefault().UtcDateTime;
        if (requestedTime.Subtract(DateTime.UtcNow).TotalHours > 24)
            throw new SecurityTokenExpiredException(
                "You must reset your password within 24 hours of requesting a password reset.");
        if (user.EmailAddress == user.UnconfirmedEmail)
        {
            user.UnconfirmedEmail = null;
            user.EmailConfirmationToken = null;
        }

        user.PasswordResetToken = null;
        user.Password = SecurityHelper.CreatePasswordHash(request.Password);
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task ChangePassword(int userId, ChangePasswordRequestDTO request)
    {
        var user = _context.User.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User does not exist.", HttpStatusCode.Unauthorized);
        if (request is {Password: null, OldPassword: null})
            throw new AuthenticationException("Both passwords need to be filled in.");
        if (!SecurityHelper.VerifyPassword(request.OldPassword!, request.Password!))
            throw new SecurityHelper.InvalidPasswordException("Passwords do not match.");
        user.Password = SecurityHelper.CreatePasswordHash(request.Password!);
        _context.User.Update(user);
        await _context.SaveChangesAsync();
        await _refreshTokenService.InvalidateTokens(userId,
            $"Password changed for user with id: {userId} at {DateTimeOffset.Now}");
    }

    private static bool ValidatePassword(UserLoginRequestDTO userLogin, User user)
    {
        return userLogin.Password != null && SecurityHelper.VerifyPassword(userLogin.Password, user.Password!);
    }

    private async Task UpdateLastLoginForUser(User user, bool refreshOnly = false)
    {
        if (!refreshOnly)
            user.LastLogin = DateTimeOffset.Now;
        user.LastRefreshTokenIssued = DateTimeOffset.Now;
        _context.User.Update(user);
        await _context.SaveChangesAsync();
    }

    private string GetResendConfirmTableEmail(User user)
    {
        var s = new StringBuilder();
        s.Append($"<table {EmailHelper.TableProperties(TableSize.Small)}>");
        s.Append("<thead>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableHeader(2)}>New User Registration</th>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>First Name</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.FirstName}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>Surname</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.Surname}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>Username</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.Username}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<td {EmailHelper.TableFooter(2)}></td>");
        s.Append("</tr>");
        s.Append("</thead>");
        s.Append("</table>");
        s.Append(
            $"<p style='font-family: sans-serif'>Please click here to <a href='{_frontendStrings.BaseUrl}/ConfirmEmail?{user.EmailConfirmationToken}'>confirm your email.</a></p>");
        return s.ToString();
    }

    private string GetForgotPasswordTableEmail(User user)
    {
        var s = new StringBuilder();
        s.Append($"<table {EmailHelper.TableProperties(TableSize.Small)}>");
        s.Append("<thead>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableHeader(2)}>Password Reset</th>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>First Name</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.FirstName}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>Surname</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.Surname}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<th {EmailHelper.TableCell}>Username</th>");
        s.Append($"<td {EmailHelper.TableCell}>{user.Username}</td>");
        s.Append("</tr>");
        s.Append("<tr>");
        s.Append($"<td {EmailHelper.TableFooter(2)}></td>");
        s.Append("</tr>");
        s.Append("</thead>");
        s.Append("</table>");
        s.Append(
            $"<p style='font-family: sans-serif'>Please click here to <a href='{_frontendStrings.BaseUrl}/ResetPassword?{user.PasswordResetToken}'>reset your password.</a></p>");
        return s.ToString();
    }
}