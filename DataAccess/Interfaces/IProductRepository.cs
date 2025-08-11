using Domain.DTOs.Common;
using Domain.DTOs.Product;
using Domain.Entities;

namespace DataAccess.Interfaces;

public interface IProductRepository : IBaseRepository<Product, int>
{
    Task<PaginatedResponse<ProductDto>> SearchProductsAsync(ProductSearchRequest request);
    Task<bool> UpdateViewCountAsync(int productId);
    Task<IEnumerable<Product>> GetProductsByCreatorAsync(int creatorId);
    Task<bool> BulkUpdateAsync(BulkUpdateProductsRequest request, int updatedBy);
    Task<IEnumerable<Product>> GetProductsByTagsAsync(string[] tags);
    Task<bool> UpdateStockAsync(int productId, int newStock);
    Task<decimal> GetAveragePriceAsync();
    Task<int> GetTotalStockAsync();
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
}