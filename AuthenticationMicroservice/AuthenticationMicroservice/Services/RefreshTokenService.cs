namespace AuthenticationMicroservice.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.DTOs;
using Models.Entities;
using Repositories;
using Settings;

public interface IRefreshTokenService
{
    Task<AccessToken> CreateNewTokenForUser(int userId);
    AccessToken CreateNewTokenForUser(User user);
    Task<RefreshToken?> GenerateRefreshToken(int userId);
    Task<RefreshToken?> RefreshToken(string? oldToken, int userId);
    Task<RefreshToken?> GetRefreshTokenForUser(int userId);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    int GetUserIdFromExpiredToken(string token);
    Task InvalidateTokens(int userId, string reason);
    Task<List<Claim>> GetClaimsForUser(int userId);
    List<Claim> GetClaimsForUser(User user);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AuthenticationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBaseRepository<RefreshToken> _refreshTokenRepo;
    private readonly TokenSettings _tokenSettings;

    public RefreshTokenService(IOptions<TokenSettings> tokenSettings, AuthenticationDbContext context,
        IBaseRepository<RefreshToken> refreshTokenRepo,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tokenSettings = tokenSettings.Value;
        _refreshTokenRepo = refreshTokenRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AccessToken> CreateNewTokenForUser(int userId)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User not found.", HttpStatusCode.Unauthorized);

        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = GetClaimsForUser(user);

        var subject = new ClaimsIdentity(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = subject,
            Expires = DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenValidityMinutes),
            SigningCredentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AccessToken
        {
            Token = tokenHandler.WriteToken(token),
            ExpiresIn = _tokenSettings.AccessTokenValidityMinutes * 60
        };
    }

    public AccessToken CreateNewTokenForUser(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = GetClaimsForUser(user);

        var subject = new ClaimsIdentity(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = subject,
            Expires = DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenValidityMinutes),
            SigningCredentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AccessToken
        {
            Token = tokenHandler.WriteToken(token),
            ExpiresIn = _tokenSettings.AccessTokenValidityMinutes * 60
        };
    }

    public async Task<List<Claim>> GetClaimsForUser(int userId)
    {
        var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new AuthenticationException("User not found.", HttpStatusCode.Unauthorized);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, user.Id.ToString()),
            new(ClaimTypes.Email, user.EmailAddress?.ToLower() ?? string.Empty),
            new(Constants.Claims.Firstname, user.FirstName ?? string.Empty),
            new(Constants.Claims.Surname, user.Surname ?? string.Empty),
            new(Constants.Claims.Username, user.Username),
            new(Constants.Claims.UserSignUpDate, user.CreatedAt.ToUnixTimeSeconds().ToString())
        };

        return claims;
    }

    public List<Claim> GetClaimsForUser(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, user.Id.ToString()),
            new(ClaimTypes.Email, user.EmailAddress?.ToLower() ?? string.Empty),
            new(Constants.Claims.Firstname, user.FirstName ?? string.Empty),
            new(Constants.Claims.Surname, user.Surname ?? string.Empty),
            new(Constants.Claims.Username, user.Username),
            new(Constants.Claims.UserSignUpDate, user.CreatedAt.ToUnixTimeSeconds().ToString())
        };

        return claims;
    }

    public async Task<RefreshToken?> RefreshToken(string? oldToken, int userId)
    {
        if (string.IsNullOrEmpty(oldToken))
            throw new UnauthorizedAccessException("Invalid token. Empty string");

        var old = await _refreshTokenRepo.GetAll().FirstOrDefaultAsync(t => t.UserId == userId && t.Token == oldToken);

        if (old == null)
            throw new UnauthorizedAccessException("Invalid token. Token doesn't exist");

        if (!old.IsValid)
            throw new UnauthorizedAccessException("Invalid token. Token no longer valid");

        if (old.Expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new UnauthorizedAccessException("Invalid token. Token expired");

        old.IsValid = false;
        old.Deleted = true;
        old.Reason = "Exchanged for new token";

        await _refreshTokenRepo.UpdateAndSaveAsync(old);

        return await GenerateRefreshToken(userId);
    }


    public async Task<RefreshToken?> GenerateRefreshToken(int userId)
    {
        var randomNumber = new byte[_tokenSettings.RefreshTokenRandomNumbers];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        var newToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            UserId = userId,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_tokenSettings.RefreshTokenValidityMinutes)
                .ToUnixTimeSeconds(),
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };

        if (!_tokenSettings.AllowMultipleRefreshTokens)
        {
            var tokens = await _refreshTokenRepo.GetAll().Where(t => t.UserId == userId && t.IsValid && !t.Deleted)
                .ToListAsync();
            await InvalidateTokens(tokens, "Deleted. New token issued. Token Settings disallow multiple");
        }

        await _refreshTokenRepo.CreateAndSaveAsync(newToken);

        return newToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenForUser(int userId)
    {
        return await _refreshTokenRepo.GetAll()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsValid && !t.Deleted);
    }

    public int GetUserIdFromExpiredToken(string token)
    {
        var principal = GetPrincipalFromExpiredToken(token);

        if (principal == null) throw new UnauthorizedAccessException("Invalid user");
        var claim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid);

        return int.Parse(claim?.Value ?? string.Empty);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    public async Task InvalidateTokens(int userId, string reason)
    {
        var existingTokens = await _refreshTokenRepo.GetAll().Where(t => t.UserId == userId && t.IsValid).ToListAsync();

        await InvalidateTokens(existingTokens, reason);
    }

    public AuthenticatedUserDTO? GetAuthenticatedUserFromExpiredToken(string token)
    {
        var principal = GetPrincipalFromExpiredToken(token);

        if (principal == null) return null;
        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid);
        var identityAccessTokenClaim =
            principal.Claims.FirstOrDefault(c => c.Type == Constants.Claims.AccessToken);
        var identityRefreshTokenClaim =
            principal.Claims.FirstOrDefault(c => c.Type == Constants.Claims.RefreshToken);

        if (identityAccessTokenClaim == null || identityRefreshTokenClaim == null)
            return null;

        return new AuthenticatedUserDTO
        {
            UserId = int.Parse(userIdClaim?.Value ?? string.Empty),
            AccessToken = identityAccessTokenClaim.Value,
            RefreshToken = identityRefreshTokenClaim.Value
        };
    }


    private async Task InvalidateTokens(List<RefreshToken> tokens, string reason)
    {
        if (tokens is {Count: > 0})
            foreach (var rt in tokens)
            {
                rt.Invalidate(reason);
                _refreshTokenRepo.PrepareToUpdate(rt);
            }

        await _refreshTokenRepo.CommitPendingChanges();
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.Secret));
    }
}