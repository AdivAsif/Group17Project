namespace AuthenticationMicroservice.Controllers;

using System.Net;
using System.Security.Claims;
using System.Text;
using Exceptions;
using Helpers;
using LazyCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Middleware;
using Models.DTOs;
using Models.Entities;
using Services;
using Settings;
using Swashbuckle.AspNetCore.Annotations;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseAuthController
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly FrontendStrings _frontendStrings;
    private readonly IAppCache _memoryCache;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserService _userService;

    public AuthController(IAppCache memoryCache, IAuthService authService, IRefreshTokenService refreshTokenService,
        IUserService userService, IEmailService emailService, IOptions<FrontendStrings> frontendStrings)
    {
        _authService = authService;
        _memoryCache = memoryCache;
        _userService = userService;
        _emailService = emailService;
        _frontendStrings = frontendStrings.Value;
        _refreshTokenService = refreshTokenService;
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<AuthenticatedUserDTO>))]
    public async Task<ActionResult<AuthenticatedUserDTO>> Authenticate([FromBody] UserLoginRequestDTO userLogin)
    {
        try
        {
            var authenticatedUser = await _authService.Authenticate(userLogin);
            if (authenticatedUser == null)
                throw new AuthenticationException("User could not be authenticated.", HttpStatusCode.Unauthorized);

            return Ok(authenticatedUser);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("RefreshToken", Name = "RefreshToken")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<AuthenticatedUserDTO>))]
    public async Task<ActionResult<AuthenticatedUserDTO>> RefreshToken([FromBody] RefreshTokenRequestDTO refresh)
    {
        if (string.IsNullOrEmpty(refresh.RefreshToken))
            throw new Exception("Refresh token cannot be null.");

        try
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return BadRequest("Missing access token");
            var accessToken = authHeader.First()?.Replace("Bearer", "").Trim();
            if (accessToken == null) return BadRequest("Missing access token.");
            var newRefreshToken = await _authService.RefreshToken(accessToken, refresh.RefreshToken);
            return Ok(newRefreshToken);
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
    [SwaggerResponse(204)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
    {
        try
        {
            await _authService.ForgotPassword(request);
            return Ok(null);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("SetPassword")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> SetPassword([FromBody] SetPasswordRequestDTO request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return BadRequest("Please enter a password.");
            SecurityHelper.EnsurePasswordComplexity(request.Password, request.ConfirmPassword);
            await _authService.SetPassword(request);
        }
        catch (SecurityHelper.InvalidPasswordException invalid)
        {
            return BadRequest(invalid.Message);
        }
        catch (SecurityHelper.WeakPasswordException weak)
        {
            return BadRequest(weak.Message);
        }
        catch (SecurityTokenExpiredException expired)
        {
            return BadRequest(expired.Message);
        }
        catch (SecurityTokenException invalid)
        {
            return BadRequest(invalid.Message);
        }

        return Ok(null);
    }

    [HttpPost("ChangePassword")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
    {
        try
        {
            SecurityHelper.EnsurePasswordComplexity(request.Password!, request.ConfirmPassword!);
            await _authService.ChangePassword(UserId, request);
        }
        catch (SecurityHelper.InvalidPasswordException invalid)
        {
            return BadRequest(invalid.Message);
        }
        catch (SecurityHelper.WeakPasswordException weak)
        {
            return BadRequest(weak.Message);
        }

        return Ok(null);
    }

    [HttpPost("ChangeEmail")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> ChangeEmail([FromBody] ChangeEmailRequestDTO request)
    {
        try
        {
            await _authService.ChangeEmail(UserId, request);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }

        return Ok(null);
    }

    [Authorize]
    [SwaggerResponse(204)]
    [HttpPost("Logout", Name = "Logout")]
    public async Task<ActionResult> Logout()
    {
        try
        {
            await _authService.Logout(UserId);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(null);
    }

    [AllowAnonymous]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<List<Claim>>))]
    [HttpGet("Claims", Name = "GetUserClaims")]
    public async Task<ActionResult<ResponseEnvelope<List<Claim>>>> GetUserClaims()
    {
        return Ok(await _refreshTokenService.GetClaimsForUser(UserId));
    }

    [HttpPost("GetAccessToken/{userId:int}", Name = "GetAccessTokenForUserUsingApiKey")]
    [SwaggerOperation("Use API key to get an access token for a user.")]
    public async Task<ActionResult<ResponseEnvelope<AccessToken>>> GetAccessTokenForUserUsingApiKey(int userId)
    {
        return Ok(await _refreshTokenService.CreateNewTokenForUser(userId));
    }

    [AllowAnonymous]
    [HttpPost("CreateUser")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<AuthenticatedUserDTO>))]
    public async Task<ActionResult<ResponseEnvelope<AuthenticatedUserDTO>>> CreateUser(
        [FromBody] UserRegisterRequestDTO request)
    {
        try
        {
            var createdUser = await _userService.CreateUser(request);
            var authenticatedUser = await _authService.GenerateAuthenticatedUser(createdUser);
            if (authenticatedUser == null)
                throw new AuthenticationException("User could not be authenticated.", HttpStatusCode.Unauthorized);
            if (request.EmailAddress != null)
                _emailService.SendEmail(new EmailDTO
                {
                    Subject = "Successfully Registered", Body = GetRegisteredUserTableEmail(createdUser),
                    Receiver = request.EmailAddress,
                    To = request.Username
                }).FireAndForget();
            return Ok(authenticatedUser);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("ResendConfirmEmail")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> ResendConfirmEmail()
    {
        try
        {
            await _authService.ResendConfirmEmail(UserId);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(null);
    }

    [Authorize]
    [HttpPost("ConfirmEmail")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> ConfirmEmail(string token)
    {
        try
        {
            await _authService.ConfirmEmailAddress(UserId, token);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(null);
    }

    [AllowAnonymous]
    [HttpPost("SendEmail")]
    [SwaggerResponse(204)]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult> SendEmail(EmailDTO emailToSend)
    {
        try
        {
            _emailService.SendEmail(emailToSend).FireAndForget();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(null);
    }

    [AllowAnonymous]
    [HttpGet("GetUserById")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<UserDTO>))]
    public async Task<ActionResult<ResponseEnvelope<UserDTO>>> GetUserById(int userId)
    {
        try
        {
            return Ok(await _userService.GetUserById(userId));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("GetUsersById")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<List<UserDTO>>))]
    public async Task<ActionResult<ResponseEnvelope<List<UserDTO>>>> GetUsersById(List<int> userIds)
    {
        try
        {
            return Ok(await _userService.GetUsersById(userIds));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private string GetRegisteredUserTableEmail(User user)
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
}