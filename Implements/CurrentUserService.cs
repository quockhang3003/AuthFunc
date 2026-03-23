using System.Security.Claims;
using Domain.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Service.Interfaces;

namespace Service.Implements;

public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtService _jwtService;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IJwtService jwtService)
        {
            _httpContextAccessor = httpContextAccessor;
            _jwtService = jwtService;
        }

        public int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                
                if (int.TryParse(userIdClaim, out var userId))
                    return userId;

                // Try to get from HttpContext.Items (set by middleware)
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("UserId", out var userIdObj) == true && 
                    userIdObj is int contextUserId)
                {
                    return contextUserId;
                }

                return null;
            }
        }

        public string? Username =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ??
            _httpContextAccessor.HttpContext?.User?.FindFirst("username")?.Value;

        public long Permissions
        {
            get
            {
                var permissionsClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(AuthConstants.PERMISSION_CLAIM_TYPE)?.Value;
                if (long.TryParse(permissionsClaim, out var permissions))
                    return permissions;

                // Try to get from HttpContext.Items (set by middleware)
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("UserPermissions", out var permissionsObj) == true && 
                    permissionsObj is long contextPermissions)
                {
                    return contextPermissions;
                }

                return (long) Permission.Permissions.None;
            }
        }

        public AuthenticationType AuthType
        {
            get
            {
                var authTypeClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(AuthConstants.AUTH_TYPE_CLAIM_TYPE)?.Value;
                if (Enum.TryParse<AuthenticationType>(authTypeClaim, out var authType))
                    return authType;

                // Try to get from HttpContext.Items (set by middleware)
                if (_httpContextAccessor.HttpContext?.Items.TryGetValue("AuthType", out var authTypeObj) == true && 
                    authTypeObj is AuthenticationType contextAuthType)
                {
                    return contextAuthType;
                }

                return AuthenticationType.JWT; // Default
            }
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null) return null;

                // Check for forwarded IP first
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }

                // Check for real IP
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    return realIp;
                }

                // Fall back to connection remote IP
                return context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            }
        }

        public string? UserAgent =>
            _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].FirstOrDefault();

        public bool HasPermission(Permission.Permissions permission)
        {
            if (permission == Permission.Permissions.None) return true;
            var userPermissions = Permissions;
            return (userPermissions & (long)permission) == (long)permission;
        }

        public bool HasAllPermissions(params Permission.Permissions[] permissions)
        {
            if (permissions == null || permissions.Length == 0) return true;
            return permissions.All(HasPermission);
        }

        public bool HasAnyPermission(params Permission.Permissions[] permissions)
        {
            if (permissions == null || permissions.Length == 0) return true;
            return permissions.Any(HasPermission);
        }

        public string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        }

        public async Task<bool> ValidateCurrentTokenAsync()
        {
            var token = ExtractTokenFromHeader();
            if (string.IsNullOrEmpty(token))
                return false;

            var validation = await _jwtService.ValidateTokenAsync(token);
            return validation.IsValid;
        }

        public bool CanAccessUser(int targetUserId)
        {
            if (UserId == targetUserId)
                return true;
            
            if (HasPermission(Permission.Permissions.SystemAdmin))
                return true;
            
            return HasPermission(Permission.Permissions.ViewUsers);
        }

        public bool CanModifyUser(int targetUserId)
        {
            if (UserId == targetUserId)
                return false;

            if (HasPermission(Permission.Permissions.SystemAdmin))
                return true;

            return HasPermission(Permission.Permissions.UpdateUsers);
        }

        private string? ExtractTokenFromHeader()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader["Bearer ".Length..].Trim();
        }
    }