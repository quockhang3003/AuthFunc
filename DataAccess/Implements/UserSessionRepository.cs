using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Implements;

public class UserSessionRepository : IUserSessionRepository
{
    public Task<UserSession?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSession>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedResponse<UserSession>> GetPagedAsync(int pageNumber, int pageSize, string? whereClause = null, object? parameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<int> InsertAsync(UserSession entity)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(UserSession entity)
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

    public Task<UserSession?> GetBySessionIdAsync(string sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSession>> GetActiveSessionsByUserAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeactivateSessionAsync(string sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeactivateAllUserSessionsAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateLastAccessAsync(string sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<int> CleanupInactiveSessionsAsync(TimeSpan inactiveThreshold)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetActiveSessionCountAsync(int userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSession>> GetSessionsByAuthTypeAsync(AuthenticationType authType)
    {
        throw new NotImplementedException();
    }
}