using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace API.Controllers;

[Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService, ILogger<UsersController> logger, ICurrentUserService currentUserService)
            : base(logger, currentUserService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetUsers([FromQuery] UserSearchRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<PaginatedResponse<UserDto>>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<PaginatedResponse<UserDto>>(Permission.Permissions.ViewUsers);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<PaginatedResponse<UserDto>>();
            if (validation != null) return validation;

            LogUserAction("Get Users", new { 
                request.PageNumber, 
                request.PageSize, 
                request.Search 
            });

            var result = await _userService.GetUserAsync(request);
            return BuildApiResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
        {
            var authCheck = CheckAuthenticationWithResponse<UserDto>();
            if (authCheck != null) return authCheck;

            var accessCheck = CheckUserAccessWithResponse<UserDto>(id, "view");
            if (accessCheck != null) return accessCheck;

            LogUserAction("Get User", new { id });

            var result = await _userService.GetUserByIdAsync(id);
            return BuildApiResponse(result);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<UserDto>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<UserDto>(Permission.Permissions.CreateUsers);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<UserDto>();
            if (validation != null) return validation;

            // Only admins can create users with admin permissions
            if ((request.Permissions & (long)Permission.Permissions.SystemAdmin) != 0 && 
                !HasPermission(Permission.Permissions.SystemAdmin))
            {
                LogSecurityEvent("Unauthorized Admin Creation", $"User {GetCurrentUserId()} attempted to create admin user");
                return ForbiddenResponse<UserDto>("Cannot create admin users without system admin permission");
            }

            var userId = GetCurrentUserId();

            LogUserAction("Create User", new { request.Username, request.Email, request.AuthType, userId });

            var result = await _userService.CreateUserAsync(request, userId);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
            }

            return BuildApiResponse(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<UserDto>();
            if (authCheck != null) return authCheck;

            var accessCheck = CheckUserAccessWithResponse<UserDto>(id, "modify");
            if (accessCheck != null) return accessCheck;

            var validation = ValidateModelWithResponse<UserDto>();
            if (validation != null) return validation;

            var currentUserId = GetCurrentUserId();

            // Users can only modify basic info about themselves
            if (currentUserId == id && request.Permissions.HasValue)
            {
                LogSecurityEvent("Self Permission Change", $"User {currentUserId} attempted to change own permissions");
                return ForbiddenResponse<UserDto>("Cannot modify your own permissions");
            }

            LogUserAction("Update User", new { id, currentUserId, request });

            var result = await _userService.UpdateUserAsync(id, request, currentUserId);
            return BuildApiResponse(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(int id)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<string>(Permission.Permissions.DeleteUsers);
            if (permissionCheck != null) return permissionCheck;

            var currentUserId = GetCurrentUserId();

            // Cannot delete yourself
            if (currentUserId == id)
            {
                LogSecurityEvent("Self Delete Attempt", $"User {currentUserId} attempted to delete own account");
                return ForbiddenResponse<string>("Cannot delete your own account");
            }

            LogUserAction("Delete User", new { id, currentUserId });

            var result = await _userService.DeleteUserAsync(id, currentUserId);
            return BuildApiResponse(result);
        }

        [HttpPut("{id}/permissions")]
        public async Task<ActionResult<ApiResponse<string>>> ChangeUserPermissions(int id, [FromBody] ChangePermissionsRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<string>(Permission.Permissions.ManagePermissions);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<string>();
            if (validation != null) return validation;

            var currentUserId = GetCurrentUserId();

            // Cannot change your own permissions
            if (currentUserId == id)
            {
                LogSecurityEvent("Self Permission Change", $"User {currentUserId} attempted to change own permissions");
                return ForbiddenResponse<string>("Cannot modify your own permissions");
            }

            // Only system admins can grant admin permissions
            if ((request.Permissions & (long)Permission.Permissions.SystemAdmin) != 0 && 
                !HasPermission(Permission.Permissions.SystemAdmin))
            {
                LogSecurityEvent("Unauthorized Admin Grant", $"User {currentUserId} attempted to grant admin permissions to user {id}");
                return ForbiddenResponse<string>("Cannot grant admin permissions without system admin permission");
            }

            LogUserAction("Change User Permissions", new { id, request.Permissions, request.Reason, currentUserId });

            var result = await _userService.ChangeUserPermissionsAsync(id, request, currentUserId);
            return BuildApiResponse(result);
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<string>>> ToggleUserStatus(int id)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<string>(Permission.Permissions.UpdateUsers);
            if (permissionCheck != null) return permissionCheck;

            var currentUserId = GetCurrentUserId();

            // Cannot toggle your own status
            if (currentUserId == id)
            {
                LogSecurityEvent("Self Status Toggle", $"User {currentUserId} attempted to toggle own status");
                return ForbiddenResponse<string>("Cannot toggle your own status");
            }

            LogUserAction("Toggle User Status", new { id, currentUserId });

            var result = await _userService.ToggleUserStatusAsync(id, currentUserId);
            return BuildApiResponse(result);
        }

        [HttpPatch("bulk-permissions")]
        public async Task<ActionResult<ApiResponse<string>>> BulkUpdatePermissions([FromBody] BulkPermissionsRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<string>(Permission.Permissions.ManagePermissions);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<string>();
            if (validation != null) return validation;

            var currentUserId = GetCurrentUserId();

            // Prevent self-modification in bulk
            if (request.UserIds.Contains(currentUserId))
            {
                LogSecurityEvent("Bulk Self Permission Change", $"User {currentUserId} attempted to change own permissions in bulk operation");
                return ForbiddenResponse<string>("Cannot modify your own permissions in bulk operations");
            }

            // Only system admins can bulk-grant admin permissions
            if ((request.Permissions & (long)Permission.Permissions.SystemAdmin) != 0 && 
                !HasPermission(Permission.Permissions.SystemAdmin))
            {
                LogSecurityEvent("Bulk Admin Grant", $"User {currentUserId} attempted to bulk-grant admin permissions");
                return ForbiddenResponse<string>("Cannot bulk-grant admin permissions without system admin permission");
            }

            LogUserAction("Bulk Update User Permissions", new { 
                UserCount = request.UserIds.Length, 
                request.Permissions, 
                currentUserId 
            });

            var result = await _userService.BulkUpdatePermissionsAsync(request.UserIds, request.Permissions, currentUserId);
            return BuildApiResponse(result);
        }

        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            var authCheck = CheckAuthenticationWithResponse<UserDto>();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();

            LogUserAction("Get Current User", new { userId });

            var result = await _userService.GetUserByIdAsync(userId);
            return BuildApiResponse(result);
        }
    }

    public class BulkPermissionsRequest
    {
        public int[] UserIds { get; set; } = Array.Empty<int>();
        public long Permissions { get; set; }
        public string? Reason { get; set; }
    }