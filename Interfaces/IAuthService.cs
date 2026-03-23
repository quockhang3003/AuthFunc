using Domain.DTOs.Auth;
using Domain.DTOs.Common;

namespace Service.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthenticationResponse.AuthResponse>> LoginAsync(AuthenticationRequest.LoginRequest request, 
        string ipAddress, string? userAgent = null);

    Task<ApiResponse<AuthenticationResponse.AuthResponse>> WindowsLoginAsync(
        AuthenticationRequest.WindowsLoginRequest request, string windowsIdentity, string domain, string ipAddress,
        string? userAgent = null);
    Task<ApiResponse<AuthenticationResponse.AuthResponse>> RegisterAsync(AuthenticationRequest.RegisterRequest request, string ipAddress, string? userAgent = null);
    Task<ApiResponse<AuthenticationResponse.AuthResponse>> RefreshTokenAsync(string token, string ipAddress, string? accessToken = null, string? userAgent = null);
    Task<ApiResponse<string>> RevokeTokenAsync(string token, string ipAddress, string? accessToken = null, string? reason = null);
    Task<ApiResponse<string>> RevokeAllTokensAsync(int userId, string ipAddress, string? currentAccessToken = null, string? reason = null);
    Task<AuthenticationResponse.TokenValidationResponse> ValidateTokenAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
}