using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Interfaces;

public interface IUserRepository : IBaseRepository<User, int>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByWindowsIdentityAsync(string windowsIdentity);
    Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    Task<bool> WindowsIdentityExistsAsync(string windowsIdentity, int? excludeUserId = null);
    Task<PaginatedResponse<UserDto>> SearchUsersAsync(UserSearchRequest request);
    Task<bool> UpdatePermissionsAsync(int userId, long permissions);
    Task<bool> UpdateTokenVersionAsync(int userId, int newVersion);
    Task<IEnumerable<User>> GetUsersByPermissionAsync(Permission.Permissions permission);
    Task<int> GetActiveSessionCountAsync(int userId);
    Task<bool> BulkUpdatePermissionsAsync(int[] userIds, long permissions);
}