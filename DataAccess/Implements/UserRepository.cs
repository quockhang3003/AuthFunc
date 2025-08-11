using System.Text;
using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.DTOs.User;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Implements;

public class UserRepository : BaseRepository<User, int>, IUserRepository
{
    public UserRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, "users")
    {
    }

    protected override string GetSelectColumns()
    {
        return
            "id, username, email, password_hash, permissions, is_active, created_at, updated_at, token_version, windows_identity, domain, auth_type";
    }

    protected override string GetKeyColumn() => "id";

    protected override User MapToEntity(dynamic row)
    {
        return new User
        {
            Id = row.id,
            UserName = row.username,
            Email = row.email,
            PasswordHash = row.password_hash,
            Permissions = row.permissions,
            IsActive = row.is_active,
            CreatedAt = row.created_at,
            UpdatedAt = row.updated_at,
            TokenVersion = row.token_version,
            WindowsIdentity = row.windows_identity,
            Domain = row.domain,
            AuthType = (AuthenticationType)row.auth_type
        };
    }

    protected override object GetInsertParameters(User entity)
    {
        return new
        {
            username = entity.UserName,
            email = entity.Email,
            password_hash = entity.PasswordHash,
            permissions = entity.Permissions,
            is_active = entity.IsActive,
            created_at = entity.CreatedAt,
            token_version = entity.TokenVersion,
            windows_identity = entity.WindowsIdentity,
            domain = entity.Domain,
            auth_type = (int)entity.AuthType
        };
    }

    protected override object GetUpdateParameters(User entity)
    {
        return new
        {
            id = entity.Id,
            username = entity.UserName,
            email = entity.Email,
            password_hash = entity.PasswordHash,
            permissions = entity.Permissions,
            is_active = entity.IsActive,
            updated_at = DateTime.UtcNow,
            token_version = entity.TokenVersion,
            windows_identity = entity.WindowsIdentity,
            domain = entity.Domain,
            auth_type = (int)entity.AuthType
        };
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $"SELECT {GetSelectColumns()} FROM {_tableName} WHERE username = @Username AND is_active = true";
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Username = username });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $"SELECT {GetSelectColumns()} FROM {_tableName} WHERE email = @Email AND is_active = true";
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Email = email });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<User?> GetByWindowsIdentityAsync(string windowsIdentity)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql =
            $"SELECT {GetSelectColumns()} FROM {_tableName} WHERE windows_identity = @WindowsIdentity AND is_active = true";
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { WindowsIdentity = windowsIdentity });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(1) FROM users WHERE username = @Username";
        var parameters = new DynamicParameters();
        parameters.Add("Username", username);

        if (excludeUserId.HasValue)
        {
            sql += " AND id != @ExcludeUserId";
            parameters.Add("ExcludeUserId", excludeUserId.Value);
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count > 0;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(1) FROM users WHERE email = @Email";
        var parameters = new DynamicParameters();
        parameters.Add("Email", email);

        if (excludeUserId.HasValue)
        {
            sql += " AND id != @ExcludeUserId";
            parameters.Add("ExcludeUserId", excludeUserId.Value);
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count > 0;
    }

    public async Task<bool> WindowsIdentityExistsAsync(string windowsIdentity, int? excludeUserId = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(1) FROM users WHERE windows_identity = @WindowsIdentity";
        var parameters = new DynamicParameters();
        parameters.Add("WindowsIdentity", windowsIdentity);

        if (excludeUserId.HasValue)
        {
            sql += " AND id != @ExcludeUserId";
            parameters.Add("ExcludeUserId", excludeUserId.Value);
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count > 0;
    }
    

    public async Task<PaginatedResponse<UserDto>> SearchUsersAsync(UserSearchRequest request)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var whereBuilder = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        // Build dynamic WHERE clause
        if (!string.IsNullOrEmpty(request.Search))
        {
            whereBuilder.Append(" AND (username ILIKE @Search OR email ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search}%");
        }

        if (request.AuthType.HasValue)
        {
            whereBuilder.Append(" AND auth_type = @AuthType");
            parameters.Add("AuthType", (int)request.AuthType.Value);
        }

        if (request.IsActive.HasValue)
        {
            whereBuilder.Append(" AND is_active = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        if (request.HasPermission.HasValue)
        {
            whereBuilder.Append(" AND (permissions & @Permission) = @Permission");
            parameters.Add("Permission", request.HasPermission.Value);
        }

        var whereClause = whereBuilder.ToString();

        // Count query
        var countSql = $"SELECT COUNT(*) FROM users {whereClause}";
        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Data query with pagination
        var offset = request.Skip;
        var limit = request.Take;

        var dataSql = $@"
                SELECT u.id, u.username, u.email, u.permissions, u.is_active, u.created_at, u.auth_type, u.domain,
                       COALESCE(s.active_sessions, 0) as active_session_count
                FROM users u
                LEFT JOIN (
                    SELECT user_id, COUNT(*) as active_sessions 
                    FROM user_sessions 
                    WHERE is_active = true 
                    GROUP BY user_id
                ) s ON u.id = s.user_id
                {whereClause}
                ORDER BY u.id
                LIMIT @Limit OFFSET @Offset";

        parameters.Add("Limit", limit);
        parameters.Add("Offset", offset);

        var results = await connection.QueryAsync(dataSql, parameters);
        var userDtos = results.Select(MapToUserDto).ToList();

        return PaginatedResponse<UserDto>.Create(userDtos, totalCount, request.PageNumber, request.PageSize);
    }

    private UserDto MapToUserDto(dynamic row)
    {
        var permissions = (long)row.permissions;
        var permissionNames = GetPermissionNames(permissions);

        return new UserDto
        {
            Id = row.id,
            Username = row.username,
            Email = row.email,
            Permissions = permissions,
            PermissionNames = permissionNames,
            IsActive = row.is_active,
            CreatedAt = row.created_at,
            AuthType = (AuthenticationType)row.auth_type,
            Domain = row.domain,
            ActiveSessionCount = row.active_session_count ?? 0
        };
    }

    private string[] GetPermissionNames(long permissions)
    {
        var names = new List<string>();
        foreach (Permission.Permissions permission in Enum.GetValues<Permission.Permissions>())
        {
            if (permission != Permission.Permissions.None && (permissions & (long)permission) == (long)permission)
            {
                names.Add(permission.ToString());
            }
        }

        return names.ToArray();
    }

    public async Task<bool> UpdatePermissionsAsync(int userId, long permissions)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "UPDATE users SET permissions = @Permissions, updated_at = @UpdatedAt WHERE id = @UserId";
        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Permissions = permissions,
            UpdatedAt = DateTime.UtcNow
        });
        return affectedRows > 0;
    }

    public async Task<bool> UpdateTokenVersionAsync(int userId, int newVersion)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "UPDATE users SET token_version = @TokenVersion, updated_at = @UpdatedAt WHERE id = @UserId";
        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            TokenVersion = newVersion,
            UpdatedAt = DateTime.UtcNow
        });
        return affectedRows > 0;
    }
    
    public async Task<IEnumerable<User>> GetUsersByPermissionAsync(Permission.Permissions permission)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql =
            $"SELECT {GetSelectColumns()} FROM users WHERE (permissions & @Permission) = @Permission AND is_active = true";
        var results = await connection.QueryAsync(sql, new { Permission = (long)permission });
        return results.Select(MapToEntity);
    }

    public async Task<int> GetActiveSessionCountAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(*) FROM user_sessions WHERE user_id = @UserId AND is_active = true";
        return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
    }

    public async Task<bool> BulkUpdatePermissionsAsync(int[] userIds, long permissions)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "UPDATE users SET permissions = @Permissions, updated_at = @UpdatedAt WHERE id = ANY(@UserIds)";
        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserIds = userIds,
            Permissions = permissions,
            UpdatedAt = DateTime.UtcNow
        });
        return affectedRows > 0;
    }
}