using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataAccess.Interfaces;
using Domain.Constants;
using Domain.DTOs.Auth;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Service.Interfaces;

namespace Service.Implements;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;

    public JwtService(
        IConfiguration configuration,
        IBlacklistedTokenRepository blacklistedTokenRepository,
        IUserRepository userRepository,
        ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _blacklistedTokenRepository = blacklistedTokenRepository;
        _userRepository = userRepository;
        _logger = logger;

        _secret = _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        _issuer = _configuration["JwtSettings:Issuer"] ?? "JwtApiProject";
        _audience = _configuration["JwtSettings:Audience"] ?? "JwtApiProject-Users";
        _accessTokenExpiryMinutes = int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "15");
    }

    public string GenerateAccessToken(User user, int tokenVersion)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(AuthConstants.PERMISSION_CLAIM_TYPE, user.Permissions.ToString()),
            new Claim(AuthConstants.AUTH_TYPE_CLAIM_TYPE, ((int)user.AuthType).ToString()),
            new Claim(AuthConstants.TOKEN_VERSION_CLAIM_TYPE, tokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        if (user.AuthType == AuthenticationType.Windows && !string.IsNullOrEmpty(user.WindowsIdentity))
        {
            claims.Add(new Claim(AuthConstants.WINDOWS_IDENTITY_CLAIM_TYPE, user.WindowsIdentity));
            if (!string.IsNullOrEmpty(user.Domain))
            {
                claims.Add(new Claim(AuthConstants.DOMAIN_CLAIM_TYPE, user.Domain));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    public string? GetTokenId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public DateTime? GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public int? GetTokenVersion(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var versionClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == AuthConstants.TOKEN_VERSION_CLAIM_TYPE)?.Value;

            if (int.TryParse(versionClaim, out var version))
                return version;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthenticationResponse.TokenValidationResponse> ValidateTokenAsync(string token)
    {
        try
        {
            var principal = GetPrincipalFromToken(token, validateLifetime: true);
            if (principal == null)
            {
                return new AuthenticationResponse.TokenValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token"
                };
            }

            // Extract claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var permissionsClaim = principal.FindFirst(AuthConstants.PERMISSION_CLAIM_TYPE)?.Value;
            var authTypeClaim = principal.FindFirst(AuthConstants.AUTH_TYPE_CLAIM_TYPE)?.Value;
            var tokenVersionClaim = principal.FindFirst(AuthConstants.TOKEN_VERSION_CLAIM_TYPE)?.Value;
            var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!int.TryParse(userIdClaim, out var userId) ||
                !long.TryParse(permissionsClaim, out var permissions) ||
                !int.TryParse(authTypeClaim, out var authTypeInt) ||
                !int.TryParse(tokenVersionClaim, out var tokenVersion))
            {
                return new AuthenticationResponse.TokenValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token claims"
                };
            }

            // Check if token is blacklisted
            if (!string.IsNullOrEmpty(jtiClaim))
            {
                var isBlacklisted = await _blacklistedTokenRepository.IsTokenBlacklistedAsync(jtiClaim);
                if (isBlacklisted)
                {
                    return new AuthenticationResponse.TokenValidationResponse
                    {
                        IsValid = false,
                        ErrorMessage = "Token has been revoked"
                    };
                }
            }

            // Verify token version matches user's current version
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return new AuthenticationResponse.TokenValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "User not found or inactive"
                };
            }

            if (user.TokenVersion != tokenVersion)
            {
                return new AuthenticationResponse.TokenValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Token version mismatch - all tokens have been revoked"
                };
            }

            var expiry = GetTokenExpiry(token);

            return new AuthenticationResponse.TokenValidationResponse
            {
                IsValid = true,
                UserId = userId,
                Permissions = permissions,
                AuthType = (AuthenticationType)authTypeInt,
                TokenVersion = tokenVersion,
                ExpiryTime = expiry ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new AuthenticationResponse.TokenValidationResponse
            {
                IsValid = false,
                ErrorMessage = "Token validation error"
            };
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        return await _blacklistedTokenRepository.IsTokenBlacklistedAsync(tokenId);
    }
}