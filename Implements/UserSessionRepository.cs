using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Implements;

public class UserSessionRepository : BaseRepository<UserSession, int>, IUserSessionRepository
{
    public UserSessionRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory, "user_sessions")
    {
    }

    protected override string GetSelectColumns()
    {
        return "id, user_id, session_id, ip_address, user_agent, device_info, auth_type, created_at, last_access_at, is_active";
    }

    protected override string GetKeyColumn() => "id";

    protected override UserSession MapToEntity(dynamic row)
    {
        return new UserSession
        {
            Id = row.id,
            UserId = row.user_id,
            SessionId = row.session_id,
            IpAddress = row.ip_address,
            UserAgent = row.user_agent,
            DeviceInfo = row.device_info,
            AuthType = (AuthenticationType)row.auth_type,
            CreatedAt = row.created_at,
            LastAccessAt = row.last_access_at,
            IsActive = row.is_active
        };
    }

    protected override object GetInsertParameters(UserSession entity)
    {
        return new
        {
            user_id = entity.UserId,
            session_id = entity.SessionId,
            ip_address = entity.IpAddress,
            user_agent = entity.UserAgent,
            device_info = entity.DeviceInfo,
            auth_type = (int)entity.AuthType,
            created_at = entity.CreatedAt,
            last_access_at = entity.LastAccessAt,
            is_active = entity.IsActive
        };
    }

    protected override object GetUpdateParameters(UserSession entity)
    {
        return new
        {
            id = entity.Id,
            user_id = entity.UserId,
            session_id = entity.SessionId,
            ip_address = entity.IpAddress,
            user_agent = entity.UserAgent,
            device_info = entity.DeviceInfo,
            auth_type = (int)entity.AuthType,
            created_at = entity.CreatedAt,
            last_access_at = entity.LastAccessAt,
            is_active = entity.IsActive
        };
    }

    public async Task<UserSession?> GetBySessionIdAsync(string sessionId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $"SELECT {GetSelectColumns()} FROM user_sessions WHERE session_id = @SessionId";
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { SessionId = sessionId });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsByUserAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $@"
            SELECT {GetSelectColumns()} 
            FROM user_sessions 
            WHERE user_id = @UserId AND is_active = 1
            ORDER BY last_access_at DESC";

        var results = await connection.QueryAsync(sql, new { UserId = userId });
        return results.Select(MapToEntity);
    }

    public async Task<bool> DeactivateSessionAsync(string sessionId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "UPDATE user_sessions SET is_active = 0 WHERE session_id = @SessionId";
        var affectedRows = await connection.ExecuteAsync(sql, new { SessionId = sessionId });
        return affectedRows > 0;
    }

    public async Task<bool> DeactivateAllUserSessionsAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "UPDATE user_sessions SET is_active = 0 WHERE user_id = @UserId";
        var affectedRows = await connection.ExecuteAsync(sql, new { UserId = userId });
        return affectedRows > 0;
    }

    public async Task<bool> UpdateLastAccessAsync(string sessionId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            UPDATE user_sessions 
            SET last_access_at = @Now 
            WHERE session_id = @SessionId AND is_active = 1";

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            SessionId = sessionId,
            Now = DateTime.UtcNow
        });

        return affectedRows > 0;
    }

    public async Task<int> CleanupInactiveSessionsAsync(TimeSpan inactiveThreshold)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var cutoffTime = DateTime.UtcNow - inactiveThreshold;

        var sql = @"
            DELETE FROM user_sessions 
            WHERE is_active = 0 
               OR (last_access_at IS NOT NULL AND last_access_at < @CutoffTime)
               OR (last_access_at IS NULL AND created_at < @CutoffTime)";

        var affectedRows = await connection.ExecuteAsync(sql, new { CutoffTime = cutoffTime });
        return affectedRows;
    }

    public async Task<int> GetActiveSessionCountAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(*) FROM user_sessions WHERE user_id = @UserId AND is_active = 1";
        return await connection.QuerySingleAsync<int>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserSession>> GetSessionsByAuthTypeAsync(AuthenticationType authType)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $@"
            SELECT {GetSelectColumns()} 
            FROM user_sessions 
            WHERE auth_type = @AuthType AND is_active = 1
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync(sql, new { AuthType = (int)authType });
        return results.Select(MapToEntity);
    }
}