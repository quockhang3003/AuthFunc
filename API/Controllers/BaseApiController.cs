using Domain.DTOs.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;
        protected readonly ICurrentUserService _currentUserService;

        protected BaseApiController(ILogger logger, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        #region Current User Helper Methods

        protected int GetCurrentUserId()
        {
            return _currentUserService.UserId ?? 0;
        }

        protected string GetCurrentUsername()
        {
            return _currentUserService.Username ?? "Anonymous";
        }

        protected long GetCurrentUserPermissions()
        {
            return _currentUserService.Permissions;
        }

        protected Domain.Enums.AuthenticationType GetCurrentAuthType()
        {
            return _currentUserService.AuthType;
        }

        protected bool IsCurrentUserAuthenticated()
        {
            return _currentUserService.IsAuthenticated;
        }

        protected bool HasPermission(Permission.Permissions permission)
        {
            return _currentUserService.HasPermission(permission);
        }

        protected bool HasAllPermissions(params Permission.Permissions[] permissions)
        {
            return _currentUserService.HasAllPermissions(permissions);
        }

        protected bool HasAnyPermission(params Permission.Permissions[] permissions)
        {
            return _currentUserService.HasAnyPermission(permissions);
        }

        #endregion

        #region HTTP Context Helper Methods

        protected string GetIpAddress()
        {
            return _currentUserService.IpAddress ?? "Unknown";
        }

        protected string GetUserAgent()
        {
            return _currentUserService.UserAgent ?? "Unknown";
        }

        protected string? ExtractTokenFromHeader()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            return string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")
                ? null
                : authHeader["Bearer ".Length..].Trim();
        }

        protected string? GetDeviceInfo()
        {
            return Request.Headers["X-Device-Info"].FirstOrDefault() ?? GetUserAgent();
        }

        protected string GetTraceId()
        {
            return HttpContext.TraceIdentifier;
        }

        #endregion

        #region Response Helper Methods

        protected ActionResult<ApiResponse<T>> BuildApiResponse<T>(ApiResponse<T> response)
        {
            response.TraceId = GetTraceId();
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return response.Errors?.Count > 0 ? BadRequest(response) : NotFound(response);
        }

        protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Success")
        {
            var response = ApiResponse<T>.SuccessResponse(data, message);
            response.TraceId = GetTraceId();
            return Ok(response);
        }

        protected ActionResult<ApiResponse<T>> ErrorResponse<T>(string message, List<string>? errors = null)
        {
            var response = ApiResponse<T>.ErrorResponse(message, errors);
            response.TraceId = GetTraceId();
            return BadRequest(response);
        }

        protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string message = "Resource not found")
        {
            var response = ApiResponse<T>.ErrorResponse(message);
            response.TraceId = GetTraceId();
            return NotFound(response);
        }

        protected ActionResult<ApiResponse<T>> ForbiddenResponse<T>(string message = "Access denied")
        {
            var response = ApiResponse<T>.ErrorResponse(message);
            response.TraceId = GetTraceId();
            return StatusCode(403, response);
        }

        protected ActionResult<ApiResponse<T>> UnauthorizedResponse<T>(string message = "Authentication required")
        {
            var response = ApiResponse<T>.ErrorResponse(message);
            response.TraceId = GetTraceId();
            return Unauthorized(response);
        }

        #endregion

        #region Permission Check Methods

        protected ActionResult<ApiResponse<T>>? CheckPermissionWithResponse<T>(Permission.Permissions permission)
        {
            if (!HasPermission(permission))
            {
                _logger.LogWarning("Permission denied for user {UserId}. Required: {Permission}", 
                    GetCurrentUserId(), permission);
                return ForbiddenResponse<T>($"Permission denied. Required: {permission}");
            }
            return null;
        }

        protected ActionResult<ApiResponse<T>>? CheckAuthenticationWithResponse<T>()
        {
            if (!IsCurrentUserAuthenticated())
            {
                _logger.LogWarning("Unauthenticated access attempt from IP: {IP}", GetIpAddress());
                return UnauthorizedResponse<T>();
            }
            return null;
        }

        protected ActionResult<ApiResponse<T>>? CheckUserAccessWithResponse<T>(int targetUserId, string operation = "access")
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId == targetUserId)
                return null;
            
            if (HasPermission(Permission.Permissions.SystemAdmin))
                return null;
                
            if (HasPermission(Permission.Permissions.BasicUser) && operation == "view")
                return null;
            
            _logger.LogWarning("User {CurrentUserId} attempted to {Operation} user {TargetUserId}", 
                currentUserId, operation, targetUserId);
            return ForbiddenResponse<T>($"Cannot {operation} this user");
        }

        #endregion

        #region Model Validation

        protected ActionResult<ApiResponse<T>>? ValidateModelWithResponse<T>()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return ErrorResponse<T>("Validation failed", errors);
            }
            return null;
        }

        #endregion

        #region Logging Helpers

        protected void LogUserAction(string action, object? parameters = null)
        {
            _logger.LogInformation("User {UserId} ({Username}) performed {Action} from {IP}. Parameters: {@Parameters}", 
                GetCurrentUserId(), GetCurrentUsername(), action, GetIpAddress(), parameters);
        }

        protected void LogSecurityEvent(string eventType, string details)
        {
            _logger.LogWarning("SECURITY EVENT: {EventType} - User: {UserId}, IP: {IP}, Details: {Details}", 
                eventType, GetCurrentUserId(), GetIpAddress(), details);
        }

        #endregion
}