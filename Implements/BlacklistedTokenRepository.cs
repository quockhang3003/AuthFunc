using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;

namespace DataAccess.Implements;

public class BlacklistedTokenRepository : BaseRepository<BlacklistedToken, int>, IBlacklistedTokenRepository
{
    public BlacklistedTokenRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory, "blacklisted_tokens")
    {
    }

    protected override string GetSelectColumns()
    {
        return "id, token_id, expiry_date, blacklisted_at, reason, user_id, ip_address";
    }

    protected override string GetKeyColumn() => "id";

    protected override BlacklistedToken MapToEntity(dynamic row)
    {
        return new BlacklistedToken
        {
            Id = row.id,
            TokenId = row.token_id,
            ExpiryDate = row.expiry_date,
            BlacklistedAt = row.blacklisted_at,
            Reason = row.reason,
            UserId = row.user_id,
            IpAddress = row.ip_address
        };
    }

    protected override object GetInsertParameters(BlacklistedToken entity)
    {
        return new
        {
            token_id = entity.TokenId,
            expiry_date = entity.ExpiryDate,
            blacklisted_at = entity.BlacklistedAt,
            reason = entity.Reason,
            user_id = entity.UserId,
            ip_address = entity.IpAddress
        };
    }

    protected override object GetUpdateParameters(BlacklistedToken entity)
    {
        return new
        {
            id = entity.Id,
            token_id = entity.TokenId,
            expiry_date = entity.ExpiryDate,
            blacklisted_at = entity.BlacklistedAt,
            reason = entity.Reason,
            user_id = entity.UserId,
            ip_address = entity.IpAddress
        };
    }

    public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "SELECT COUNT(1) FROM blacklisted_tokens WHERE token_id = @TokenId AND expiry_date > @Now";
        var count = await connection.QuerySingleAsync<int>(sql, new
        {
            TokenId = tokenId,
            Now = DateTime.UtcNow
        });
        return count > 0;
    }

    public async Task<bool> AddToBlacklistAsync(string tokenId, DateTime expiryDate, string? reason = null,
        int? userId = null, string? ipAddress = null)
    {
        var token = new BlacklistedToken
        {
            TokenId = tokenId,
            ExpiryDate = expiryDate,
            BlacklistedAt = DateTime.UtcNow,
            Reason = reason,
            UserId = userId,
            IpAddress = ipAddress
        };

        var id = await InsertAsync(token);
        return id > 0;
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = "DELETE FROM blacklisted_tokens WHERE expiry_date < @Now";
        var affectedRows = await connection.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
        return affectedRows;
    }

    public async Task<bool> BlacklistUserTokensAsync(int userId, string reason)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var sql = @"
            INSERT INTO blacklisted_tokens (token_id, expiry_date, blacklisted_at, reason, user_id)
            SELECT rt.token, rt.expires_at, @Now, @Reason, @UserId
            FROM refresh_tokens rt
            WHERE rt.user_id = @UserId AND rt.revoked_at IS NULL AND rt.expires_at > @Now";

        var affectedRows = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Reason = reason,
            Now = DateTime.UtcNow
        });

        return affectedRows > 0;
    }

    public async Task<IEnumerable<BlacklistedToken>> GetBlacklistedTokensByUserAsync(int userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sql = $@"
            SELECT {GetSelectColumns()} 
            FROM blacklisted_tokens 
            WHERE user_id = @UserId AND expiry_date > @Now
            ORDER BY blacklisted_at DESC";

        var results = await connection.QueryAsync(sql, new
        {
            UserId = userId,
            Now = DateTime.UtcNow
        });

        return results.Select(MapToEntity);
    }
}