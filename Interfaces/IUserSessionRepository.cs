using Domain.Entities;
using Domain.Enums;

namespace DataAccess.Interfaces;

public interface IUserSessionRepository : IBaseRepository<UserSession, int>
{
    Task<UserSession?> GetBySessionIdAsync(string sessionId);
    Task<IEnumerable<UserSession>> GetActiveSessionsByUserAsync(int userId);
    Task<bool> DeactivateSessionAsync(string sessionId);
    Task<bool> DeactivateAllUserSessionsAsync(int userId);
    Task<bool> UpdateLastAccessAsync(string sessionId);
    Task<int> CleanupInactiveSessionsAsync(TimeSpan inactiveThreshold);
    Task<int> GetActiveSessionCountAsync(int userId);
    Task<IEnumerable<UserSession>> GetSessionsByAuthTypeAsync(AuthenticationType authType);
}