using DataAccess.Interfaces;
using Domain.Constants;
using Domain.DTOs.Auth;
using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using BCrypt = BCrypt.Net.BCrypt;

namespace Service.Implements;

public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IBlacklistedTokenRepository blacklistedTokenRepository,
            IUserSessionRepository userSessionRepository,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _blacklistedTokenRepository = blacklistedTokenRepository;
            _userSessionRepository = userSessionRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthenticationResponse.AuthResponse>> LoginAsync(AuthenticationRequest.LoginRequest request, string ipAddress, string? userAgent = null)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(request.Username);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Login attempt failed - user not found or inactive: {Username}", request.Username);
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Invalid username or password");
                }

                if (user.AuthType == AuthenticationType.Windows)
                {
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Please use Windows authentication for this account");
                }

                if (string.IsNullOrEmpty(user.PasswordHash) || !global::BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed - invalid password: {Username}", request.Username);
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Invalid username or password");
                }

                return await GenerateAuthResponseAsync(user, ipAddress, userAgent, request.DeviceInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("An error occurred during login");
            }
        }

        public async Task<ApiResponse<AuthenticationResponse.AuthResponse>> WindowsLoginAsync(AuthenticationRequest.WindowsLoginRequest request, string windowsIdentity, string domain, string ipAddress, string? userAgent = null)
        {
            try
            {
                var user = await _userRepository.GetByWindowsIdentityAsync(windowsIdentity);
                if (user == null)
                {
                    // Auto-create user for Windows authentication
                    user = new User
                    {
                        UserName = windowsIdentity.Split('\\').Last(), // Extract username from DOMAIN\username
                        Email = $"{windowsIdentity.Replace("\\", "_")}@{domain}",
                        WindowsIdentity = windowsIdentity,
                        Domain = domain,
                        AuthType = AuthenticationType.Windows,
                        Permissions = PermissionConstants.BASIC_USER_PERMISSIONS,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var userId = await _userRepository.InsertAsync(user);
                    user.Id = userId;
                    
                    _logger.LogInformation("Auto-created Windows user: {WindowsIdentity}", windowsIdentity);
                }
                else if (!user.IsActive)
                {
                    _logger.LogWarning("Windows login attempt failed - user inactive: {WindowsIdentity}", windowsIdentity);
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Account is inactive");
                }

                return await GenerateAuthResponseAsync(user, ipAddress, userAgent, request.DeviceInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Windows login for user: {WindowsIdentity}", windowsIdentity);
                return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("An error occurred during Windows authentication");
            }
        }

        public async Task<ApiResponse<AuthenticationResponse.AuthResponse>> RegisterAsync(AuthenticationRequest.RegisterRequest request, string ipAddress, string? userAgent = null)
        {
            try
            {
                if (await _userRepository.UsernameExistsAsync(request.Username))
                {
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Username already exists");
                }

                if (await _userRepository.EmailExistsAsync(request.Email))
                {
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Email already exists");
                }

                var user = new User
                {
                    UserName = request.Username,
                    Email = request.Email,
                    PasswordHash = global::BCrypt.Net.BCrypt.HashPassword(request.Password),
                    AuthType = AuthenticationType.JWT,
                    Permissions = PermissionConstants.BASIC_USER_PERMISSIONS,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var userId = await _userRepository.InsertAsync(user);
                user.Id = userId;

                _logger.LogInformation("User registered successfully: {Username}", request.Username);
                return await GenerateAuthResponseAsync(user, ipAddress, userAgent, request.DeviceInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
                return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("An error occurred during registration");
            }
        }

        public async Task<ApiResponse<AuthenticationResponse.AuthResponse>> RefreshTokenAsync(string token, string ipAddress, string? currentAccessToken = null, string? userAgent = null)
        {
            try
            {
                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
                if (refreshToken == null || !refreshToken.IsActive)
                {
                    _logger.LogWarning("Refresh token invalid or expired: {Token}", token[..Math.Min(10, token.Length)]);
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("Invalid refresh token");
                }

                var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
                if (user == null || !user.IsActive)
                {
                    return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("User not found or inactive");
                }

                // Blacklist current access token if provided
                if (!string.IsNullOrEmpty(currentAccessToken))
                {
                    var jti = _jwtService.GetTokenId(currentAccessToken);
                    var expiry = _jwtService.GetTokenExpiry(currentAccessToken);
                    if (!string.IsNullOrEmpty(jti) && expiry.HasValue)
                    {
                        await _blacklistedTokenRepository.AddToBlacklistAsync(jti, expiry.Value, "refresh", user.Id, ipAddress);
                    }
                }

                // Rotate refresh token
                await _refreshTokenRepository.RevokeTokenAsync(token, ipAddress, null);
                
                return await GenerateAuthResponseAsync(user, ipAddress, userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return ApiResponse<AuthenticationResponse.AuthResponse>.ErrorResponse("An error occurred during token refresh");
            }
        }

        private async Task<ApiResponse<AuthenticationResponse.AuthResponse>> GenerateAuthResponseAsync(User user, string ipAddress, string? userAgent = null, string? deviceInfo = null)
        {
            // Clean up old tokens
            var activeTokenCount = await _refreshTokenRepository.GetActiveTokenCountByUserAsync(user.Id);
            if (activeTokenCount >= PermissionConstants.MAX_REFRESH_TOKENS_PER_USER)
            {
                await _refreshTokenRepository.RevokeOldestTokenAsync(user.Id, ipAddress);
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user, user.TokenVersion);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(PermissionConstants.REFRESH_TOKEN_EXPIRY_DAYS),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserId = user.Id,
                UserAgent = userAgent,
                DeviceInfo = deviceInfo,
                AuthType = user.AuthType
            };

            await _refreshTokenRepository.InsertAsync(refreshToken);

            // Create session
            var session = new UserSession
            {
                UserId = user.Id,
                SessionId = Guid.NewGuid().ToString(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceInfo = deviceInfo,
                AuthType = user.AuthType,
                CreatedAt = DateTime.UtcNow,
                LastAccessAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userSessionRepository.InsertAsync(session);

            var permissionNames = GetPermissionNames(user.Permissions);

            var response = new AuthenticationResponse.AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(PermissionConstants.ACCESS_TOKEN_EXPIRY_MINUTES),
                AuthType = user.AuthType,
                GrantedPermissions = permissionNames,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Permissions = user.Permissions,
                    PermissionNames = permissionNames,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    AuthType = user.AuthType,
                    Domain = user.Domain
                }
            };

            return ApiResponse<AuthenticationResponse.AuthResponse>.SuccessResponse(response, "Authentication successful");
        }

        private string[] GetPermissionNames(long permissions)
        {
            var names = new List<string>();
            foreach (Permission.Permissions permission in Enum.GetValues<Permission.Permissions>())
            {
                if (permission != Permission.Permissions.None && (permissions & (long)permission) == (long)permission)
                {
                    names.Add(permission.ToString());
                }
            }
            return names.ToArray();
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync(string token, string ipAddress, string? accessToken = null, string? reason = null)
        {
            try
            {
                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
                if (refreshToken == null)
                {
                    return ApiResponse<string>.ErrorResponse("Invalid refresh token");
                }

                await _refreshTokenRepository.RevokeTokenAsync(token, ipAddress);

                if (!string.IsNullOrEmpty(accessToken))
                {
                    var jti = _jwtService.GetTokenId(accessToken);
                    var expiry = _jwtService.GetTokenExpiry(accessToken);
                    if (!string.IsNullOrEmpty(jti) && expiry.HasValue)
                    {
                        await _blacklistedTokenRepository.AddToBlacklistAsync(jti, expiry.Value, reason ?? "revoke", refreshToken.UserId, ipAddress);
                    }
                }

                return ApiResponse<string>.SuccessResponse("Token revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return ApiResponse<string>.ErrorResponse("An error occurred while revoking token");
            }
        }

        public async Task<ApiResponse<string>> RevokeAllTokensAsync(int userId, string ipAddress, string? currentAccessToken = null, string? reason = null)
        {
            try
            {
                // Revoke all refresh tokens
                await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, ipAddress);

                // Blacklist current access token
                if (!string.IsNullOrEmpty(currentAccessToken))
                {
                    var jti = _jwtService.GetTokenId(currentAccessToken);
                    var expiry = _jwtService.GetTokenExpiry(currentAccessToken);
                    if (!string.IsNullOrEmpty(jti) && expiry.HasValue)
                    {
                        await _blacklistedTokenRepository.AddToBlacklistAsync(jti, expiry.Value, reason ?? "revoke_all", userId, ipAddress);
                    }
                }

                // Increment token version to invalidate all existing access tokens
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    await _userRepository.UpdateTokenVersionAsync(userId, user.TokenVersion + 1);
                }

                // Deactivate all sessions
                await _userSessionRepository.DeactivateAllUserSessionsAsync(userId);

                return ApiResponse<string>.SuccessResponse("All tokens revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
                return ApiResponse<string>.ErrorResponse("An error occurred while revoking all tokens");
            }
        }

        public async Task<AuthenticationResponse.TokenValidationResponse> ValidateTokenAsync(string token)
        {
            try
            {
                var validation = await _jwtService.ValidateTokenAsync(token);
                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return new AuthenticationResponse.TokenValidationResponse 
                { 
                    IsValid = false, 
                    ErrorMessage = "Token validation failed" 
                };
            }
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var validation = await ValidateTokenAsync(token);
            return validation.IsValid;
        }
    }