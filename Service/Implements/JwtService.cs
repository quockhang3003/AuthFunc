using System.Security.Claims;
using Domain.DTOs.Auth;
using Domain.Entities;
using Service.Interfaces;

namespace Service.Implements;

public class JwtService : IJwtService
{
    public string GenerateAccessToken(User user, int tokenVersion)
    {
        throw new NotImplementedException();
    }

    public string GenerateRefreshToken()
    {
        throw new NotImplementedException();
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true)
    {
        throw new NotImplementedException();
    }

    public string? GetTokenId(string token)
    {
        throw new NotImplementedException();
    }

    public DateTime? GetTokenExpiry(string token)
    {
        throw new NotImplementedException();
    }

    public int? GetTokenVersion(string token)
    {
        throw new NotImplementedException();
    }

    public Task<AuthenticationResponse.TokenValidationResponse> ValidateTokenAsync(string token)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        throw new NotImplementedException();
    }
}