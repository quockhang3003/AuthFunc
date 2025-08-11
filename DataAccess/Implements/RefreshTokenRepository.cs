using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Implements;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<RefreshToken>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedResponse<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, string? whereClause = null, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> InsertAsync(RefreshToken entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(RefreshToken entity)
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

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeTokenAsync(string token, string revokedByIp, string? replacedByToken = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeAllUserTokensAsync(int userId, string revokedByIp)
    {
        throw new NotImplementedException();
    }

    public Task<int> CleanupExpiredTokensAsync()
    {
        throw new NotImplementedException();
    }

    public Task<int> GetActiveTokenCountByUserAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RevokeOldestTokenAsync(int userId, string revokedByIp)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<RefreshToken>> GetTokensByAuthTypeAsync(AuthenticationType authType)
    {
        throw new NotImplementedException();
    }
}