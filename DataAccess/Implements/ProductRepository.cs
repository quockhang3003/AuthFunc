using System.Text;
using System.Text.Json;
using Dapper;
using DataAccess.Interfaces;
using Domain.DTOs.Common;
using Domain.DTOs.Product;
using Domain.Entities;

namespace DataAccess.Implements;

public class ProductRepository : BaseRepository<Product, int>, IProductRepository
    {
        public ProductRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory, "products")
        {
        }

        protected override string GetSelectColumns()
        {
            return "id, name, description, price, stock, is_active, created_at, updated_at, created_by, updated_by, tags, view_count";
        }

        protected override string GetKeyColumn() => "id";

        protected override Product MapToEntity(dynamic row)
        {
            return new Product
            {
                Id = row.id,
                Name = row.name,
                Description = row.description ?? string.Empty,
                Price = row.price,
                Stock = row.stock,
                IsActive = row.is_active,
                CreatedAt = row.created_at,
                UpdatedAt = row.updated_at,
                CreatedBy = row.created_by,
                UpdatedBy = row.updated_by,
                Tags = row.tags,
                ViewCount = row.view_count ?? 0
            };
        }

        protected override object GetInsertParameters(Product entity)
        {
            return new
            {
                name = entity.Name,
                description = entity.Description,
                price = entity.Price,
                stock = entity.Stock,
                is_active = entity.IsActive,
                created_at = entity.CreatedAt,
                created_by = entity.CreatedBy,
                tags = entity.Tags,
                view_count = entity.ViewCount
            };
        }

        protected override object GetUpdateParameters(Product entity)
        {
            return new
            {
                id = entity.Id,
                name = entity.Name,
                description = entity.Description,
                price = entity.Price,
                stock = entity.Stock,
                is_active = entity.IsActive,
                updated_at = DateTime.UtcNow,
                updated_by = entity.UpdatedBy,
                tags = entity.Tags,
                view_count = entity.ViewCount
            };
        }

        public async Task<PaginatedResponse<ProductDto>> SearchProductsAsync(ProductSearchRequest request)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            var whereBuilder = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();
            
            // Build dynamic WHERE clause
            if (!string.IsNullOrEmpty(request.Search))
            {
                whereBuilder.Append(" AND (name ILIKE @Search OR description ILIKE @Search OR tags::text ILIKE @Search)");
                parameters.Add("Search", $"%{request.Search}%");
            }

            if (request.MinPrice.HasValue)
            {
                whereBuilder.Append(" AND price >= @MinPrice");
                parameters.Add("MinPrice", request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                whereBuilder.Append(" AND price <= @MaxPrice");
                parameters.Add("MaxPrice", request.MaxPrice.Value);
            }

            if (!request.IncludeInactive)
            {
                whereBuilder.Append(" AND is_active = true");
            }
            else if (request.IsActive.HasValue)
            {
                whereBuilder.Append(" AND is_active = @IsActive");
                parameters.Add("IsActive", request.IsActive.Value);
            }

            if (request.Tags != null && request.Tags.Length > 0)
            {
                whereBuilder.Append(" AND tags::jsonb ?| @Tags");
                parameters.Add("Tags", request.Tags);
            }

            var whereClause = whereBuilder.ToString();

            // Count query
            var countSql = $"SELECT COUNT(*) FROM products {whereClause}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            // Build ORDER BY clause
            var orderBy = request.SortBy?.ToLower() switch
            {
                "name" => "name",
                "price" => "price",
                "stock" => "stock",
                "created_at" => "created_at",
                "view_count" => "view_count",
                _ => "id"
            };

            var sortDirection = request.SortDescending ? "DESC" : "ASC";
            var orderClause = $"ORDER BY {orderBy} {sortDirection}";

            // Data query with pagination
            var offset = request.Skip;
            var limit = request.Take;
            
            var dataSql = $@"
                SELECT p.id, p.name, p.description, p.price, p.stock, p.is_active, p.created_at, p.updated_at, p.view_count, p.tags,
                       u.username as created_by_username,
                       u2.username as updated_by_username
                FROM products p
                INNER JOIN users u ON p.created_by = u.id
                LEFT JOIN users u2 ON p.updated_by = u2.id
                {whereClause}
                {orderClause}
                LIMIT @Limit OFFSET @Offset";

            parameters.Add("Limit", limit);
            parameters.Add("Offset", offset);

            var results = await connection.QueryAsync(dataSql, parameters);
            var productDtos = results.Select(MapToProductDto).ToList();

            return PaginatedResponse<ProductDto>.Create(productDtos, totalCount, request.PageNumber, request.PageSize);
        }

        private ProductDto MapToProductDto(dynamic row)
        {
            string[]? tags = null;
            if (row.tags != null)
            {
                try
                {
                    tags = JsonSerializer.Deserialize<string[]>(row.tags.ToString());
                }
                catch
                {
                    tags = null;
                }
            }

            return new ProductDto
            {
                Id = row.id,
                Name = row.name,
                Description = row.description ?? string.Empty,
                Price = row.price,
                Stock = row.stock,
                IsActive = row.is_active,
                CreatedAt = row.created_at,
                CreatedBy = row.created_by_username ?? "Unknown",
                UpdatedAt = row.updated_at,
                UpdatedBy = row.updated_by_username,
                Tags = tags,
                ViewCount = row.view_count ?? 0
            };
        }

        public async Task<bool> UpdateViewCountAsync(int productId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "UPDATE products SET view_count = COALESCE(view_count, 0) + 1 WHERE id = @ProductId";
            var affectedRows = await connection.ExecuteAsync(sql, new { ProductId = productId });
            return affectedRows > 0;
        }

        public async Task<IEnumerable<Product>> GetProductsByCreatorAsync(int creatorId)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT {GetSelectColumns()} FROM products WHERE created_by = @CreatorId ORDER BY created_at DESC";
            var results = await connection.QueryAsync(sql, new { CreatorId = creatorId });
            return results.Select(MapToEntity);
        }

        public async Task<bool> BulkUpdateAsync(BulkUpdateProductsRequest request, int updatedBy)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var updates = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("ProductIds", request.ProductIds);
                parameters.Add("UpdatedBy", updatedBy);
                parameters.Add("UpdatedAt", DateTime.UtcNow);

                if (request.Price.HasValue)
                {
                    updates.Add("price = @Price");
                    parameters.Add("Price", request.Price.Value);
                }

                if (request.Stock.HasValue)
                {
                    updates.Add("stock = @Stock");
                    parameters.Add("Stock", request.Stock.Value);
                }

                if (request.IsActive.HasValue)
                {
                    updates.Add("is_active = @IsActive");
                    parameters.Add("IsActive", request.IsActive.Value);
                }

                if (request.Tags != null)
                {
                    updates.Add("tags = @Tags::jsonb");
                    parameters.Add("Tags", JsonSerializer.Serialize(request.Tags));
                }

                if (!updates.Any())
                {
                    return false;
                }

                updates.Add("updated_by = @UpdatedBy");
                updates.Add("updated_at = @UpdatedAt");

                var setClause = string.Join(", ", updates);
                var sql = $"UPDATE products SET {setClause} WHERE id = ANY(@ProductIds)";

                var affectedRows = await connection.ExecuteAsync(sql, parameters, transaction);
                transaction.Commit();
                
                return affectedRows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetProductsByTagsAsync(string[] tags)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT {GetSelectColumns()} FROM products WHERE tags::jsonb ?| @Tags AND is_active = true";
            var results = await connection.QueryAsync(sql, new { Tags = tags });
            return results.Select(MapToEntity);
        }

        public async Task<bool> UpdateStockAsync(int productId, int newStock)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "UPDATE products SET stock = @Stock, updated_at = @UpdatedAt WHERE id = @ProductId";
            var affectedRows = await connection.ExecuteAsync(sql, new 
            { 
                ProductId = productId, 
                Stock = newStock, 
                UpdatedAt = DateTime.UtcNow 
            });
            return affectedRows > 0;
        }

        public async Task<decimal> GetAveragePriceAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT AVG(price) FROM products WHERE is_active = true";
            var result = await connection.QuerySingleOrDefaultAsync<decimal?>(sql);
            return result ?? 0;
        }

        public async Task<int> GetTotalStockAsync()
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = "SELECT SUM(stock) FROM products WHERE is_active = true";
            var result = await connection.QuerySingleOrDefaultAsync<int?>(sql);
            return result ?? 0;
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var sql = $"SELECT {GetSelectColumns()} FROM products WHERE stock <= @Threshold AND is_active = true ORDER BY stock ASC";
            var results = await connection.QueryAsync(sql, new { Threshold = threshold });
            return results.Select(MapToEntity);
        }
    }