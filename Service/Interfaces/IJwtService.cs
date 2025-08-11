using System.Security.Claims;
using Domain.DTOs.Auth;
using Domain.Entities;

namespace Service.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, int tokenVersion);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);
    string? GetTokenId(string token);
    DateTime? GetTokenExpiry(string token);
    int? GetTokenVersion(string token);
    Task<AuthenticationResponse.TokenValidationResponse> ValidateTokenAsync(string token);
    Task<bool> IsTokenBlacklistedAsync(string tokenId);
}