using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Enums;

namespace Service.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PaginatedResponse<UserDto>>> GetUserAsync(UserSearchRequest request);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createBy);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request, int updatedBy);
    Task<ApiResponse<string>> DeleteUserAsync(int id, int deletedBy);
    Task<ApiResponse<string>> ChangeUserPermissionsAsync(int id, ChangePermissionsRequest request, int changedBy);
    Task<ApiResponse<string>> ToggleUserStatusAsync(int id, int changedBy);
    Task<ApiResponse<string>> BulkUpdatePermissionsAsync(int[] userIds, long permissions, int updatedBy);
    Task<bool> HasPermissionAsync(int userId, Permission.Permissions permission);
    Task<bool> ValidateUserAccessAsync(int userId, int requestingUserId, Permission.Permissions requiredPermission);
}
