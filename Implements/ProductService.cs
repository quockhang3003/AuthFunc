using System.Text.Json;
using DataAccess.Interfaces;
using Domain.Constants;
using Domain.DTOs.Common;
using Domain.DTOs.Product;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Implements;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IUserRepository userRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<PaginatedResponse<ProductDto>>> GetProductsAsync(ProductSearchRequest request)
    {
        try
        {
            var result = await _productRepository.SearchProductsAsync(request);
            return ApiResponse<PaginatedResponse<ProductDto>>.SuccessResponse(
                result, "Products retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return ApiResponse<PaginatedResponse<ProductDto>>.ErrorResponse("Error retrieving products");
        }
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id, bool incrementViewCount = false)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return ApiResponse<ProductDto>.ErrorResponse("Product not found");
            }

            // Increment view count if requested
            if (incrementViewCount)
            {
                await _productRepository.UpdateViewCountAsync(id);
                product.ViewCount++;
            }

            // Get creator info
            var creator = await _userRepository.GetByIdAsync(product.CreatedBy);
            var updater = product.UpdatedBy.HasValue
                ? await _userRepository.GetByIdAsync(product.UpdatedBy.Value)
                : null;

            string[]? tags = null;
            if (!string.IsNullOrEmpty(product.Tags))
            {
                try
                {
                    tags = JsonSerializer.Deserialize<string[]>(product.Tags);
                }
                catch
                {
                    tags = null;
                }
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CreatedBy = creator?.UserName ?? "Unknown",
                UpdatedAt = product.UpdatedAt,
                UpdatedBy = updater?.UserName,
                Tags = tags,
                ViewCount = product.ViewCount
            };

            return ApiResponse<ProductDto>.SuccessResponse(productDto, "Product retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return ApiResponse<ProductDto>.ErrorResponse("Error retrieving product");
        }
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request, int createdBy)
    {
        try
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
                ViewCount = 0
            };

            var productId = await _productRepository.InsertAsync(product);
            product.Id = productId;

            _logger.LogInformation("Product created: {ProductId} by {UserId}", productId, createdBy);

            var creator = await _userRepository.GetByIdAsync(createdBy);

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CreatedBy = creator?.UserName ?? "Unknown",
                Tags = request.Tags,
                ViewCount = 0
            };

            return ApiResponse<ProductDto>.SuccessResponse(productDto, "Product created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return ApiResponse<ProductDto>.ErrorResponse("Error creating product");
        }
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request, int updatedBy)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return ApiResponse<ProductDto>.ErrorResponse("Product not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                product.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                product.Description = request.Description;
            }

            if (request.Price.HasValue)
            {
                product.Price = request.Price.Value;
            }

            if (request.Stock.HasValue)
            {
                product.Stock = request.Stock.Value;
            }

            if (request.IsActive.HasValue)
            {
                product.IsActive = request.IsActive.Value;
            }

            if (request.Tags != null)
            {
                product.Tags = JsonSerializer.Serialize(request.Tags);
            }

            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = updatedBy;

            var success = await _productRepository.UpdateAsync(product);
            if (!success)
            {
                return ApiResponse<ProductDto>.ErrorResponse("Failed to update product");
            }

            _logger.LogInformation("Product updated: {ProductId} by {UserId}", id, updatedBy);

            // Get creator and updater info
            var creator = await _userRepository.GetByIdAsync(product.CreatedBy);
            var updater = await _userRepository.GetByIdAsync(updatedBy);

            string[]? tags = null;
            if (!string.IsNullOrEmpty(product.Tags))
            {
                try
                {
                    tags = JsonSerializer.Deserialize<string[]>(product.Tags);
                }
                catch
                {
                    tags = null;
                }
            }

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CreatedBy = creator?.UserName ?? "Unknown",
                UpdatedAt = product.UpdatedAt,
                UpdatedBy = updater?.UserName,
                Tags = tags,
                ViewCount = product.ViewCount
            };

            return ApiResponse<ProductDto>.SuccessResponse(productDto, "Product updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return ApiResponse<ProductDto>.ErrorResponse("Error updating product");
        }
    }

    public async Task<ApiResponse<string>> DeleteProductAsync(int id, int deletedBy)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return ApiResponse<string>.ErrorResponse("Product not found");
            }

            var success = await _productRepository.DeleteAsync(id);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to delete product");
            }

            _logger.LogInformation("Product deleted: {ProductId} by {UserId}", id, deletedBy);
            return ApiResponse<string>.SuccessResponse("Product deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return ApiResponse<string>.ErrorResponse("Error deleting product");
        }
    }

    public async Task<ApiResponse<string>> BulkUpdateProductsAsync(BulkUpdateProductsRequest request, int updatedBy)
    {
        try
        {
            var success = await _productRepository.BulkUpdateAsync(request, updatedBy);
            if (!success)
            {
                return ApiResponse<string>.ErrorResponse("Failed to update products");
            }

            _logger.LogInformation(
                "Bulk product update: {Count} products by {UserId}",
                request.ProductIds.Length, updatedBy);

            return ApiResponse<string>.SuccessResponse(
                $"{request.ProductIds.Length} products updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk product update");
            return ApiResponse<string>.ErrorResponse("Error updating products");
        }
    }

    public async Task<ApiResponse<IEnumerable<ProductDto>>> GetProductsByCreatorAsync(int creatorId)
    {
        try
        {
            var products = await _productRepository.GetProductsByCreatorAsync(creatorId);
            var creator = await _userRepository.GetByIdAsync(creatorId);

            var productDtos = products.Select(p =>
            {
                string[]? tags = null;
                if (!string.IsNullOrEmpty(p.Tags))
                {
                    try
                    {
                        tags = JsonSerializer.Deserialize<string[]>(p.Tags);
                    }
                    catch
                    {
                        tags = null;
                    }
                }

                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = creator?.UserName ?? "Unknown",
                    UpdatedAt = p.UpdatedAt,
                    Tags = tags,
                    ViewCount = p.ViewCount
                };
            }).ToList();

            return ApiResponse<IEnumerable<ProductDto>>.SuccessResponse(
                productDtos, "Products retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by creator {CreatorId}", creatorId);
            return ApiResponse<IEnumerable<ProductDto>>.ErrorResponse("Error retrieving products");
        }
    }

    public async Task<bool> CanUserModifyProductAsync(int productId, int userId, long userPermissions)
    {
        try
        {
            // Check if user has system admin permission
            if ((userPermissions & (long)Permission.Permissions.SystemAdmin) == (long)Permission.Permissions.SystemAdmin)
            {
                return true;
            }

            // Check if user has product manager permission
            if ((userPermissions & (long)Permission.Permissions.ProductManager) == (long)Permission.Permissions.ProductManager)
            {
                return true;
            }

            // Check if user is the creator of the product
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            return product.CreatedBy == userId &&
                   ((userPermissions & (long)Permission.Permissions.UpdateProducts) == (long)Permission.Permissions.UpdateProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking product modification permission");
            return false;
        }
    }

    public async Task<ApiResponse<object>> GetProductStatisticsAsync()
    {
        try
        {
            var totalProducts = await _productRepository.CountAsync();
            var activeProducts = await _productRepository.CountAsync("is_active = true");
            var averagePrice = await _productRepository.GetAveragePriceAsync();
            var totalStock = await _productRepository.GetTotalStockAsync();
            var lowStockProducts = await _productRepository.GetLowStockProductsAsync(10);

            var statistics = new
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                InactiveProducts = totalProducts - activeProducts,
                AveragePrice = averagePrice,
                TotalStock = totalStock,
                LowStockCount = lowStockProducts.Count(),
                LowStockProducts = lowStockProducts.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Stock,
                    p.Price
                }).Take(5) // Top 5 low stock products
            };

            return ApiResponse<object>.SuccessResponse(statistics, "Statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product statistics");
            return ApiResponse<object>.ErrorResponse("Error retrieving statistics");
        }
    }
}