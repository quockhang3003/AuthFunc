using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Service.Interfaces;

namespace API.Middleware;

public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtBlacklistMiddleware> _logger;

        public JwtBlacklistMiddleware(RequestDelegate next, ILogger<JwtBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            var path = context.Request.Path.Value?.ToLower();

            if (ShouldSkipMiddleware(path))
            {
                await _next(context);
                return;
            }

            var token = ExtractTokenFromHeader(context);
            if (!string.IsNullOrEmpty(token))
            {
                var tokenId = ExtractTokenId(token);
                if (!string.IsNullOrEmpty(tokenId))
                {
                    var isBlacklisted = await jwtService.IsTokenBlacklistedAsync(tokenId);
                    if (isBlacklisted)
                    {
                        _logger.LogWarning("Blacklisted token attempt from {IP}: {TokenId}", 
                            GetClientIP(context), tokenId);
                        
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"message\":\"Token has been revoked\",\"success\":false}");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private static bool ShouldSkipMiddleware(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/windows-login",
                "/api/auth/refresh-token",
                "/api/products", // Allow anonymous product viewing
                "/swagger",
                "/health",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }

        private static string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;
            return authHeader["Bearer ".Length..].Trim();
        }

        private static string? ExtractTokenId(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                return jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch
            {
                return null;
            }
        }

        private static string GetClientIP(HttpContext context)
        {
            return context.Request.Headers.ContainsKey("X-Forwarded-For")
                ? context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim()
                : context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
        }
    }