using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;

namespace DataAccess.Implements;

public class BlacklistedTokenRepository : IBlacklistedTokenRepository
{
    public Task<BlacklistedToken?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BlacklistedToken>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedResponse<BlacklistedToken>> GetPagedAsync(int pageNumber, int pageSize, string? whereClause = null, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> InsertAsync(BlacklistedToken entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(BlacklistedToken entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountAsync(string? whereClause = null, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AddToBlacklistAsync(string tokenId, DateTime expiryDate, string? reason = null, int? userId = null,
        string? ipAddress = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> CleanupExpiredTokensAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> BlacklistUserTokensAsync(int userId, string reason)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<BlacklistedToken>> GetBlacklistedTokensByUserAsync(int userId)
    {
        throw new NotImplementedException();
    }
}