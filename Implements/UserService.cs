using DataAccess.Interfaces;
using Domain.Constants;
using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Implements;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<PaginatedResponse<UserDto>>> GetUserAsync(UserSearchRequest request)
    {
        try
        {
            var result = await _userRepository.SearchUsersAsync(request);
            return ApiResponse<PaginatedResponse<UserDto>>.SuccessResponse(result, "Users retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return ApiResponse<PaginatedResponse<UserDto>>.ErrorResponse("Error retrieving users");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.ErrorResponse("User not found");
            }

            var sessionCount = await _userRepository.GetActiveSessionCountAsync(id);
            var permissionNames = GetPermissionNames(user.Permissions);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Permissions = user.Permissions,
                PermissionNames = permissionNames,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                AuthType = user.AuthType,
                Domain = user.Domain,
                ActiveSessionCount = sessionCount
            };

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse("Error retrieving user");
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request, int createdBy)
    {
        try
        {
            // Validate username
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return ApiResponse<UserDto>.ErrorResponse("Username already exists");
            }

            // Validate email
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return ApiResponse<UserDto>.ErrorResponse("Email already exists");
            }

            // Validate Windows identity if provided
            if (request.AuthType == AuthenticationType.Windows)
            {
                if (string.IsNullOrEmpty(request.WindowsIdentity))
                {
                    return ApiResponse<UserDto>.ErrorResponse("Windows identity is required for Windows authentication");
                }

                if (await _userRepository.WindowsIdentityExistsAsync(request.WindowsIdentity))
                {
                    return ApiResponse<UserDto>.ErrorResponse("Windows identity already exists");
                }
            }
            else
            {
                // JWT auth requires password
                if (string.IsNullOrEmpty(request.Password))
                {
                    return ApiResponse<UserDto>.ErrorResponse("Password is required for JWT authentication");
                }
            }

            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,
                PasswordHash = !string.IsNullOrEmpty(request.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(request.Password)
                    : null,
                Permissions = request.Permissions,
                AuthType = request.AuthType,
                WindowsIdentity = request.WindowsIdentity,
                Domain = request.Domain,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var userId = await _userRepository.InsertAsync(user);
            user.Id = userId;

            _logger.LogInformation("User created: {UserId} by {CreatedBy}", userId, createdBy);

            var permissionNames = GetPermissionNames(user.Permissions);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Permissions = user.Permissions,
                PermissionNames = permissionNames,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                AuthType = user.AuthType,
                Domain = user.Domain,
                ActiveSessionCount = 0
            };

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ApiResponse<UserDto>.ErrorResponse("Error creating user");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request, int updatedBy)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.ErrorResponse("User not found");
            }

            // Validate username if changed
            if (!string.IsNullOrEmpty(request.Username) && request.Username != user.UserName)
            {
                if (await _userRepository.UsernameExistsAsync(request.Username, id))
                {
                    return ApiResponse<UserDto>.ErrorResponse("Username already exists");
                }
                user.UserName = request.Username;
            }

            // Validate email if changed
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _userRepository.EmailExistsAsync(request.Email, id))
                {
                    return ApiResponse<UserDto>.ErrorResponse("Email already exists");
                }
                user.Email = request.Email;
            }

            // Update other fields
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.Permissions.HasValue)
            {
                user.Permissions = request.Permissions.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var success = await _userRepository.UpdateAsync(user);
            if (!success)
            {
                return ApiResponse<UserDto>.ErrorResponse("Failed to update user");
            }

            _logger.LogInformation("User updated: {UserId} by {UpdatedBy}", id, updatedBy);

            var sessionCount = await _userRepository.GetActiveSessionCountAsync(id);
            var permissionNames = GetPermissionNames(user.Permissions);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Permissions = user.Permissions,
                PermissionNames = permissionNames,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                AuthType = user.AuthType,
                Domain = user.Domain,
                ActiveSessionCount = sessionCount
            };

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse("Error updating user");
        }
    }

    public async Task<ApiResponse<string>> DeleteUserAsync(int id, int deletedBy)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found");
            }

            var success = await _userRepository.DeleteAsync(id);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to delete user");
            }

            _logger.LogInformation("User deleted: {UserId} by {DeletedBy}", id, deletedBy);
            return ApiResponse<string>.SuccessResponse("User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ApiResponse<string>.ErrorResponse("Error deleting user");
        }
    }

    public async Task<ApiResponse<string>> ChangeUserPermissionsAsync(int id, ChangePermissionsRequest request, int changedBy)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found");
            }

            var success = await _userRepository.UpdatePermissionsAsync(id, request.Permissions);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to update permissions");
            }

            _logger.LogInformation(
                "Permissions changed for user {UserId} by {ChangedBy}. Reason: {Reason}",
                id, changedBy, request.Reason ?? "N/A");

            return ApiResponse<string>.SuccessResponse("Permissions updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing permissions for user {UserId}", id);
            return ApiResponse<string>.ErrorResponse("Error changing permissions");
        }
    }

    public async Task<ApiResponse<string>> ToggleUserStatusAsync(int id, int changedBy)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<string>.ErrorResponse("User not found");
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var success = await _userRepository.UpdateAsync(user);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to toggle user status");
            }

            _logger.LogInformation(
                "User status toggled: {UserId} by {ChangedBy}. New status: {Status}",
                id, changedBy, user.IsActive ? "Active" : "Inactive");

            return ApiResponse<string>.SuccessResponse(
                $"User status changed to {(user.IsActive ? "Active" : "Inactive")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for user {UserId}", id);
            return ApiResponse<string>.ErrorResponse("Error toggling user status");
        }
    }

    public async Task<ApiResponse<string>> BulkUpdatePermissionsAsync(int[] userIds, long permissions, int updatedBy)
    {
        try
        {
            var success = await _userRepository.BulkUpdatePermissionsAsync(userIds, permissions);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to update permissions");
            }

            _logger.LogInformation(
                "Bulk permissions update: {Count} users by {UpdatedBy}",
                userIds.Length, updatedBy);

            return ApiResponse<string>.SuccessResponse(
                $"Permissions updated for {userIds.Length} users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk permission update");
            return ApiResponse<string>.ErrorResponse("Error updating permissions");
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, Permission.Permissions permission)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            return (user.Permissions & (long)permission) == (long)permission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ValidateUserAccessAsync(int userId, int requestingUserId, Permission.Permissions requiredPermission)
    {
        try
        {
            // Users can always access their own data
            if (userId == requestingUserId)
            {
                return true;
            }

            return await HasPermissionAsync(requestingUserId, requiredPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user access");
            return false;
        }
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