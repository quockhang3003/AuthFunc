using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Enums;
using Service.Interfaces;

namespace Service.Implements;

public class UserService : IUserService
{
    public Task<ApiResponse<PaginatedResponse<UserDto>>> GetUserAsync(UserSearchRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request, int updatedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> DeleteUserAsync(int id, int deletedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> ChangeUserPermissionsAsync(int id, ChangePermissionsRequest request, int changedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> ToggleUserStatusAsync(int id, int changedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> BulkUpdatePermissionsAsync(int[] userIds, long permissions, int updatedBy)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(int userId, Permission.Permissions permission)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateUserAccessAsync(int userId, int requestingUserId, Permission.Permissions requiredPermission)
    {
        throw new NotImplementedException();
    }
}