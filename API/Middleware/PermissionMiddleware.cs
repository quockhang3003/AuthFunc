using System.DirectoryServices.Protocols;
using System.Security.Claims;
using Domain.Constants;
using Service.Interfaces;

namespace API.Middleware;

public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var token = ExtractTokenFromHeader(context);
                if (!string.IsNullOrEmpty(token))
                {
                    var validation = await jwtService.ValidateTokenAsync(token);
                    if (validation.IsValid)
                    {
                        // Add custom claims to HttpContext
                        var identity = context.User.Identity as ClaimsIdentity;
                        
                        // Add permission claims
                        identity?.AddClaim(new Claim(AuthConstants.PERMISSION_CLAIM_TYPE, validation.Permissions.ToString()));
                        identity?.AddClaim(new Claim(AuthConstants.AUTH_TYPE_CLAIM_TYPE, validation.AuthType.ToString()));
                        identity?.AddClaim(new Claim(AuthConstants.TOKEN_VERSION_CLAIM_TYPE, validation.TokenVersion.ToString()));
                        
                        // Store in HttpContext.Items for easy access
                        context.Items["UserId"] = validation.UserId;
                        context.Items["UserPermissions"] = validation.Permissions;
                        context.Items["AuthType"] = typeof(AuthType);
                        context.Items["TokenVersion"] = validation.TokenVersion;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid token validation for user, clearing authentication");
                        context.User = new ClaimsPrincipal(); // Clear invalid authentication
                    }
                }
            }

            await _next(context);
        }

        private static string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;
            return authHeader["Bearer ".Length..].Trim();
        }
    }