using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Implements;

public class RefreshTokenRepository : BaseRepository<RefreshToken, int>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory, "refresh_tokens")
    {
    }

    protected override string GetSelectColumns()
    {
        return @"id, token, expires_at, created_at, created_by_ip, revoked_at, 
                 revoked_by_ip, replaced_by_token, user_id, user_agent, device_info, auth_type";
    }

    protected override string GetKeyColumn() => "id";

    protected override RefreshToken MapToEntity(dynamic row)
    {
        return new RefreshToken
        {
            Id = row.id,
            Token = row.token,
            ExpiresAt = row.expires_at,
            CreatedAt = row.created_at,
            CreatedByIp = row.created_by_ip,
            RevokedAt = row.revoked_at,
            RevokedByIp = row.revoked_by_ip ?? string.Empty,
            ReplacedByToken = row.replaced_by_token ?? string.Empty,
            UserId = row.user_id,
            UserAgent = row.user_agent,
            DeviceInfo = row.device_info,
            AuthType = (AuthenticationType)row.auth_type
        };
    }

    protected override object GetInsertParameters(RefreshToken entity)
    {
        return new
        {
            token = entity.Token,
            expires_at = entity.ExpiresAt,
            created_at = entity.CreatedAt,
            created_by_ip = entity.CreatedByIp,
            revoked_at = entity.RevokedAt,
            revoked_by_ip = entity.RevokedByIp,
            replaced_by_token = entity.ReplacedByToken,
            user_id = entity.UserId,
            user_agent = entity.UserAgent,
            device_info = entity.DeviceInfo,
            auth_type = (int)entity.AuthType
        };
    }

    protected override object GetUpdateParameters(RefreshToken entity)
    {
        return new
        {
            id = entity.Id,
            token = entity.Token,
            expires_at = entity.ExpiresAt,
            created_at = entity.CreatedAt,
            created_by_ip = entity.CreatedByIp,
            revoked_at = entity.RevokedAt,
            revoked_by_ip = entity.RevokedByIp,
            replaced_by_token = entity.ReplacedByToken,
            user_id = entity.UserId,
            user_agent = entity.UserAgent,
            device_info = entity.DeviceInfo,
            auth_type = (int)entity.AuthType
        };
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $"SELECT {GetSelectColumns()} FROM refresh_tokens WHERE token = @Token";
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Token = token });
        return result != null ? MapToEntity(result) : null;
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $@"
            SELECT {GetSelectColumns()} 
            FROM refresh_tokens 
            WHERE user_id = @UserId 
              AND revoked_at IS NULL 
              AND expires_at > @Now
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync(sql, new
        {
            UserId = userId,
            Now = DateTime.UtcNow
        });

        return results.Select(MapToEntity);
    }

    public async Task<bool> RevokeTokenAsync(string token, string revokedByIp, string? replacedByToken = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            UPDATE refresh_tokens 
            SET revoked_at = @Now, 
                revoked_by_ip = @RevokedByIp,
                replaced_by_token = @ReplacedByToken
            WHERE token = @Token";

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            Token = token,
            RevokedByIp = revokedByIp,
            ReplacedByToken = replacedByToken ?? string.Empty,
            Now = DateTime.UtcNow
        });

        return affectedRows > 0;
    }

    public async Task<bool> RevokeAllUserTokensAsync(int userId, string revokedByIp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            UPDATE refresh_tokens 
            SET revoked_at = @Now, 
                revoked_by_ip = @RevokedByIp
            WHERE user_id = @UserId 
              AND revoked_at IS NULL";

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            RevokedByIp = revokedByIp,
            Now = DateTime.UtcNow
        });

        return affectedRows > 0;
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "DELETE FROM refresh_tokens WHERE expires_at < @Now";
        var affectedRows = await connection.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
        return affectedRows;
    }

    public async Task<int> GetActiveTokenCountByUserAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = @"
            SELECT COUNT(*) 
            FROM refresh_tokens 
            WHERE user_id = @UserId 
              AND revoked_at IS NULL 
              AND expires_at > @Now";

        return await connection.QuerySingleAsync<int>(sql, new
        {
            UserId = userId,
            Now = DateTime.UtcNow
        });
    }

    public async Task<bool> RevokeOldestTokenAsync(int userId, string revokedByIp)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Get the oldest active token
        var sql = @"
            UPDATE refresh_tokens 
            SET revoked_at = @Now, 
                revoked_by_ip = @RevokedByIp
            WHERE id = (
                SELECT TOP 1 id 
                FROM refresh_tokens 
                WHERE user_id = @UserId 
                  AND revoked_at IS NULL 
                  AND expires_at > @Now
                ORDER BY created_at ASC
                
            )";

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            RevokedByIp = revokedByIp,
            Now = DateTime.UtcNow
        });

        return affectedRows > 0;
    }

    public async Task<IEnumerable<RefreshToken>> GetTokensByAuthTypeAsync(AuthenticationType authType)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $@"
            SELECT {GetSelectColumns()} 
            FROM refresh_tokens 
            WHERE auth_type = @AuthType 
              AND revoked_at IS NULL 
              AND expires_at > @Now
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync(sql, new
        {
            AuthType = (int)authType,
            Now = DateTime.UtcNow
        });

        return results.Select(MapToEntity);
    }
}