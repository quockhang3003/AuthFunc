using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Interfaces;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, int>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId);
    Task<bool> RevokeTokenAsync(string token, string revokedByIp, string? replacedByToken = null);
    Task<bool> RevokeAllUserTokensAsync(int userId, string revokedByIp);
    Task<int> CleanupExpiredTokensAsync();
    Task<int> GetActiveTokenCountByUserAsync(int userId);
    Task<bool> RevokeOldestTokenAsync(int userId, string revokedByIp);
    Task<IEnumerable<RefreshToken>> GetTokensByAuthTypeAsync(AuthenticationType authType);
}