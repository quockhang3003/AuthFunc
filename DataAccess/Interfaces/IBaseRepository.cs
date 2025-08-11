using Domain.DTOs.Common;

namespace DataAccess.Interfaces;

public interface IBaseRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<PaginatedResponse<TEntity>> GetPagedAsync(int pageNumber, int pageSize, string? whereClause = null, object? parameters = null);
    Task<TKey> InsertAsync(TEntity entity);
    Task<bool> UpdateAsync(TEntity entity);
    Task<bool> DeleteAsync(TKey id);
    Task<bool> ExistsAsync(TKey id);
    Task<int> CountAsync(string? whereClause = null, object? parameters = null);
}