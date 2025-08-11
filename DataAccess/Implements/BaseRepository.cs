using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;

namespace DataAccess.Implements;

public abstract class BaseRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly IDbConnectionFactory _connectionFactory;
        protected readonly string _tableName;

        protected BaseRepository(IDbConnectionFactory connectionFactory, string tableName)
        {
            _connectionFactory = connectionFactory;
            _tableName = tableName;
        }

        protected abstract string GetSelectColumns();
        protected abstract TEntity MapToEntity(dynamic row);
        protected abstract object GetInsertParameters(TEntity entity);
        protected abstract object GetUpdateParameters(TEntity entity);
        protected abstract string GetKeyColumn();

        public virtual async Task<TEntity?> GetByIdAsync(TKey id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT {GetSelectColumns()} FROM {_tableName} WHERE {GetKeyColumn()} = @Id";
            var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
            return result != null ? MapToEntity(result) : null;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT {GetSelectColumns()} FROM {_tableName}";
            var results = await connection.QueryAsync(sql);
            return results.Select(MapToEntity);
        }

        public virtual async Task<PaginatedResponse<TEntity>> GetPagedAsync(int pageNumber, int pageSize, string? whereClause = null, object? parameters = null)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            var whereStr = string.IsNullOrEmpty(whereClause) ? "" : $"WHERE {whereClause}";
            var countSql = $"SELECT COUNT(*) FROM {_tableName} {whereStr}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
            
            var offset = (pageNumber - 1) * pageSize;
            var dynamicParams = new DynamicParameters(parameters);
            dynamicParams.Add("PageSize", pageSize);
            dynamicParams.Add("Offset", offset);
            
            var dataSql = $"SELECT {GetSelectColumns()} FROM {_tableName} {whereStr} LIMIT @PageSize OFFSET @Offset";
            var results = await connection.QueryAsync(dataSql,dynamicParams);
            
            var entities = results.Select(MapToEntity).ToList();
            return PaginatedResponse<TEntity>.Create(entities, totalCount, pageNumber, pageSize);
        }

        public virtual async Task<TKey> InsertAsync(TEntity entity)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var parameters = GetInsertParameters(entity);
            var columns = GetInsertColumns(parameters);
            var values = GetInsertValues(parameters);
            
            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}) RETURNING {GetKeyColumn()}";
            var result = await connection.QuerySingleAsync<TKey>(sql, parameters);
            return result;
        }

        public virtual async Task<bool> UpdateAsync(TEntity entity)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var parameters = GetUpdateParameters(entity);
            var setClause = GetUpdateSetClause(parameters);
            
            var sql = $"UPDATE {_tableName} SET {setClause} WHERE {GetKeyColumn()} = @{GetKeyColumn()}";
            var affectedRows = await connection.ExecuteAsync(sql, parameters);
            return affectedRows > 0;
        }

        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"DELETE FROM {_tableName} WHERE {GetKeyColumn()} = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        public virtual async Task<bool> ExistsAsync(TKey id)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {GetKeyColumn()} = @Id";
            var count = await connection.QuerySingleAsync<int>(sql, new { Id = id });
            return count > 0;
        }

        public virtual async Task<int> CountAsync(string? whereClause = null, object? parameters = null)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var whereStr = string.IsNullOrEmpty(whereClause) ? "" : $"WHERE {whereClause}";
            var sql = $"SELECT COUNT(*) FROM {_tableName} {whereStr}";
            return await connection.QuerySingleAsync<int>(sql, parameters);
        }

        protected virtual string GetInsertColumns(object parameters)
        {
            var properties = parameters.GetType().GetProperties()
                .Where(p => p.Name != GetKeyColumn())
                .Select(p => p.Name);
            return string.Join(", ", properties);
        }

        protected virtual string GetInsertValues(object parameters)
        {
            var properties = parameters.GetType().GetProperties()
                .Where(p => p.Name != GetKeyColumn())
                .Select(p =>
                {
                    if (p.Name.Equals("Tags", StringComparison.OrdinalIgnoreCase))
                        return "@" + p.Name + "::jsonb";
                    return "@" + p.Name;
                });
            return string.Join(", ", properties);
        }

        protected virtual string GetUpdateSetClause(object parameters)
        {
            var properties = parameters.GetType().GetProperties()
                .Where(p => p.Name != GetKeyColumn())
                .Select(p => $"{p.Name} = @{p.Name}");
            return string.Join(", ", properties);
        }
    }