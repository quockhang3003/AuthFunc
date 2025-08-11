using Domain.DTOs.Auth;
using Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace API.Controllers;

public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, ICurrentUserService currentUserService)
            : base(logger, currentUserService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthenticationResponse.AuthResponse>>> Login([FromBody] AuthenticationRequest.LoginRequest request)
        {
            var validation = ValidateModelWithResponse<AuthenticationResponse.AuthResponse>();
            if (validation != null) return validation;

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var deviceInfo = GetDeviceInfo();
            
            request.DeviceInfo = deviceInfo;

            LogUserAction("Login Attempt", new { request.Username, ipAddress, userAgent });

            var result = await _authService.LoginAsync(request, ipAddress, userAgent);

            if (!result.Success)
            {
                LogSecurityEvent("Failed Login", $"Username: {request.Username}, IP: {ipAddress}");
            }
            else
            {
                SetRefreshTokenCookie(result.Data!.RefreshToken);
            }

            return BuildApiResponse(result);
        }

        [HttpPost("windows-login")]
        public async Task<ActionResult<ApiResponse<AuthenticationResponse.AuthResponse>>> WindowsLogin([FromBody] AuthenticationRequest.WindowsLoginRequest request)
        {
            // Extract Windows identity from HttpContext
            var windowsIdentity = HttpContext.User.Identity;
            if (windowsIdentity == null || !windowsIdentity.IsAuthenticated)
            {
                LogSecurityEvent("Windows Auth Failed", "No Windows identity found");
                return UnauthorizedResponse<AuthenticationResponse.AuthResponse>("Windows authentication required");
            }

            var identity = windowsIdentity.Name;
            if (string.IsNullOrEmpty(identity))
            {
                LogSecurityEvent("Windows Auth Failed", "Empty Windows identity");
                return UnauthorizedResponse<AuthenticationResponse.AuthResponse>("Invalid Windows identity");
            }

            // Extract domain from identity (DOMAIN\username format)
            var parts = identity.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : Environment.UserDomainName;
            var windowsId = parts.Length > 1 ? identity : $"{domain}\\{identity}";

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var deviceInfo = GetDeviceInfo();
            
            request.DeviceInfo = deviceInfo;

            LogUserAction("Windows Login Attempt", new { WindowsIdentity = windowsId, Domain = domain, ipAddress, userAgent });

            var result = await _authService.WindowsLoginAsync(request, windowsId, domain, ipAddress, userAgent);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.Data!.RefreshToken);
            }
            else
            {
                LogSecurityEvent("Failed Windows Login", $"Identity: {windowsId}, IP: {ipAddress}");
            }

            return BuildApiResponse(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthenticationResponse.AuthResponse>>> Register([FromBody] AuthenticationRequest.RegisterRequest request)
        {
            var validation = ValidateModelWithResponse<AuthenticationResponse.AuthResponse>();
            if (validation != null) return validation;

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();
            var deviceInfo = GetDeviceInfo();
            
            request.DeviceInfo = deviceInfo;

            LogUserAction("Registration Attempt", new { request.Username, request.Email, ipAddress });

            var result = await _authService.RegisterAsync(request, ipAddress, userAgent);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.Data!.RefreshToken);
            }

            return BuildApiResponse(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthenticationResponse.AuthResponse>>> RefreshToken([FromBody] AuthenticationRequest.RefreshTokenRequest? request = null)
        {
            var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return ErrorResponse<AuthenticationResponse.AuthResponse>("Refresh token is required");
            }

            var accessToken = ExtractTokenFromHeader();
            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, accessToken, userAgent);

            if (result.Success)
            {
                SetRefreshTokenCookie(result.Data!.RefreshToken);
            }

            return BuildApiResponse(result);
        }

        [HttpPost("revoke-token")]
        public async Task<ActionResult<ApiResponse<string>>> RevokeToken([FromBody] AuthenticationRequest.RevokeTokenRequest? request = null)
        {
            var token = request?.Token ?? Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(token))
            {
                return ErrorResponse<string>("Token is required");
            }

            var ipAddress = GetIpAddress();
            var accessToken = ExtractTokenFromHeader();
            var reason = request?.Reason ?? "user_request";

            LogUserAction("Token Revocation", new { reason, ipAddress });

            var result = await _authService.RevokeTokenAsync(token, ipAddress, accessToken, reason);
            return BuildApiResponse(result);
        }

        [HttpPost("revoke-all-tokens")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> RevokeAllTokens()
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();
            var ipAddress = GetIpAddress();
            var accessToken = ExtractTokenFromHeader();

            LogUserAction("All Tokens Revocation", new { userId, ipAddress });

            var result = await _authService.RevokeAllTokensAsync(userId, ipAddress, accessToken, "user_request");
            return BuildApiResponse(result);
        }

        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<string>>> Logout()
        {
            var token = ExtractTokenFromHeader();
            if (string.IsNullOrEmpty(token))
            {
                return ErrorResponse<string>("No token provided");
            }

            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var ipAddress = GetIpAddress();
                await _authService.RevokeTokenAsync(refreshToken, ipAddress, token, "logout");
            }

            // Clear cookie
            Response.Cookies.Delete("refreshToken");

            LogUserAction("Logout", new { ip = GetIpAddress() });

            return SuccessResponse("Logged out successfully");
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> LogoutAll()
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();
            var ipAddress = GetIpAddress();
            var accessToken = ExtractTokenFromHeader();

            await _authService.RevokeAllTokensAsync(userId, ipAddress, accessToken, "logout_all");

            // Clear cookie
            Response.Cookies.Delete("refreshToken");

            LogUserAction("Logout All Devices", new { userId, ipAddress });

            return SuccessResponse("Logged out from all devices successfully");
        }

        [HttpGet("validate-token")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ValidateToken()
        {
            var authCheck = CheckAuthenticationWithResponse<object>();
            if (authCheck != null) return authCheck;

            var token = ExtractTokenFromHeader();
            if (string.IsNullOrEmpty(token))
            {
                return ErrorResponse<object>("No token provided");
            }

            var validation = await _authService.ValidateTokenAsync(token);
            
            if (!validation.IsValid)
            {
                return ErrorResponse<object>(validation.ErrorMessage ?? "Token is invalid");
            }

            var result = new
            {
                UserId = validation.UserId,
                Permissions = validation.Permissions,
                AuthType = validation.AuthType,
                ExpiryTime = validation.ExpiryTime,
                IsValid = true
            };

            return SuccessResponse((object)result, "Token is valid");
        }

        #region Helper Methods

        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,
                Path = "/"
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        #endregion
    }