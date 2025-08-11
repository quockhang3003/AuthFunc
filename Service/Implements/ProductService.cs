using Domain.DTOs.Common;
using Domain.DTOs.Product;
using Service.Interfaces;

namespace Service.Implements;

public class ProductService : IProductService
{
    public Task<ApiResponse<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductSearchRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id, bool incrementViewCount = false)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request, int createdBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int updatedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> DeleteProductAsync(int id, int deletedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<string>> BulkUpdateProductsAsync(BulkUpdateProductsRequest request, int updatedBy)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<IEnumerable<ProductDto>>> GetProductsByCreatorAsync(int creatorId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CanUserModifyProductAsync(int productId, int userId, long userPermissions)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<object>> GetProductStatisticsAsync()
    {
        throw new NotImplementedException();
    }
}