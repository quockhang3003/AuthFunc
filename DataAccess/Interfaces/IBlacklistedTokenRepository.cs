using Domain.Entities;

namespace DataAccess.Interfaces;

public interface IBlacklistedTokenRepository : IBaseRepository<BlacklistedToken, int>
{
    Task<bool> IsTokenBlacklistedAsync(string tokenId);
    Task<bool> AddToBlacklistAsync(string tokenId, DateTime expiryDate, string? reason = null, int? userId = null, string? ipAddress = null);
    Task<int> CleanupExpiredTokensAsync();
    Task<bool> BlacklistUserTokensAsync(int userId, string reason);
    Task<IEnumerable<BlacklistedToken>> GetBlacklistedTokensByUserAsync(int userId);
}