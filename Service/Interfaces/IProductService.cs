using Domain.DTOs.Common;
using Domain.DTOs.Product;

namespace Service.Interfaces;

public interface IProductService
{
    Task<ApiResponse<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductSearchRequest request);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id, bool incrementViewCount = false);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request, int createdBy);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int updatedBy);
    Task<ApiResponse<string>> DeleteProductAsync(int id, int deletedBy);
    Task<ApiResponse<string>> BulkUpdateProductsAsync(BulkUpdateProductsRequest request, int updatedBy);
    Task<ApiResponse<IEnumerable<ProductDto>>> GetProductsByCreatorAsync(int creatorId);
    Task<bool> CanUserModifyProductAsync(int productId, int userId, long userPermissions);
    Task<ApiResponse<object>> GetProductStatisticsAsync();
}