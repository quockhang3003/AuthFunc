using Domain.DTOs.ActiveDirectory;
using Domain.DTOs.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActiveDirectoryController : BaseApiController
    {
        private readonly IActiveDirectoryService _adService;

        public ActiveDirectoryController(
            IActiveDirectoryService adService,
            ILogger<ActiveDirectoryController> logger,
            ICurrentUserService currentUserService)
            : base(logger, currentUserService)
        {
            _adService = adService;
        }

        [HttpGet("test-connection")]
        public async Task<ActionResult<ApiResponse<object>>> TestConnection()
        {
            var authCheck = CheckAuthenticationWithResponse<object>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<object>(Permission.Permissions.SystemAdmin);
            if (permissionCheck != null) return permissionCheck;

            try
            {
                var currentUsername = GetCurrentUsername();
                var isValid = await _adService.ValidateUserAsync(currentUsername);

                return SuccessResponse<object>(new
                {
                    Connected = true,
                    IsValid = isValid,
                    Message = "Active Directory connection successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD connection test failed");
                return ErrorResponse<object>("AD connection failed: " + ex.Message);
            }
        }

        [HttpGet("users/{username}")]
        public async Task<ActionResult<ApiResponse<AdUserInfo>>> GetAdUser(string username)
        {
            var authCheck = CheckAuthenticationWithResponse<AdUserInfo>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<AdUserInfo>(Permission.Permissions.ViewUsers);
            if (permissionCheck != null) return permissionCheck;

            LogUserAction("Get AD User Info", new { username });

            var userInfo = await _adService.GetUserInfoAsync(username);

            if (userInfo == null)
            {
                return NotFoundResponse<AdUserInfo>($"User {username} not found in Active Directory");
            }

            return SuccessResponse(userInfo);
        }

        [HttpGet("users/{username}/groups")]
        public async Task<ActionResult<ApiResponse<List<AdGroupInfo>>>> GetUserGroups(string username)
        {
            var authCheck = CheckAuthenticationWithResponse<List<AdGroupInfo>>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<List<AdGroupInfo>>(Permission.Permissions.ViewUsers);
            if (permissionCheck != null) return permissionCheck;

            LogUserAction("Get AD User Groups", new { username });

            var groups = await _adService.GetUserGroupsAsync(username);

            return SuccessResponse(groups);
        }

        [HttpPost("users/{username}/permissions")]
        public async Task<ActionResult<ApiResponse<object>>> GetUserPermissions(string username)
        {
            var authCheck = CheckAuthenticationWithResponse<object>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<object>(Permission.Permissions.ViewUsers);
            if (permissionCheck != null) return permissionCheck;

            var groups = await _adService.GetUserGroupsAsync(username);
            var groupNames = groups.Select(g => g.Name).ToList();
            var permissions = await _adService.MapGroupsToPermissionsAsync(groupNames);

            return SuccessResponse<object>(new
            {
                Username = username,
                Groups = groupNames,
                Permissions = permissions,
                PermissionNames = GetPermissionNames(permissions)
            });
        }

        private string[] GetPermissionNames(long permissions)
        {
            var names = new List<string>();
            foreach (Permission.Permissions permission in Enum.GetValues<Permission.Permissions>())
            {
                if (permission != Permission.Permissions.None &&
                    (permissions & (long)permission) == (long)permission)
                {
                    names.Add(permission.ToString());
                }
            }
            return names.ToArray();
        }
    }
}
