using Domain.DTOs.Common;
using Domain.DTOs.Product;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace API.Controllers;

[Authorize]
    public class ProductsController : BaseApiController
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger, ICurrentUserService currentUserService)
            : base(logger, currentUserService)
        {
            _productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductDto>>>> GetProducts([FromQuery] ProductSearchRequest request)
        {
            var validation = ValidateModelWithResponse<PaginatedResponse<ProductDto>>();
            if (validation != null) return validation;

           
            if (!IsCurrentUserAuthenticated())
            {
                request.IncludeInactive = false;
            }
            
            else if (request.IncludeInactive && !HasPermission(Permission.Permissions.ProductManager))
            {
                request.IncludeInactive = false;
            }

            LogUserAction("Get Products", new { 
                request.PageNumber, 
                request.PageSize, 
                request.Search, 
                request.IncludeInactive 
            });

            var result = await _productService.GetProductsAsync(request);
            return BuildApiResponse(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id, [FromQuery] bool incrementViewCount = false)
        {
            LogUserAction("Get Product", new { id, incrementViewCount });

            var result = await _productService.GetProductByIdAsync(id, incrementViewCount);
            return BuildApiResponse(result);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<ProductDto>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<ProductDto>(Permission.Permissions.CreateProducts);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<ProductDto>();
            if (validation != null) return validation;

            var userId = GetCurrentUserId();

            LogUserAction("Create Product", new { request.Name, request.Price, userId });

            var result = await _productService.CreateProductAsync(request, userId);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetProduct), new { id = result.Data!.Id }, result);
            }

            return BuildApiResponse(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<ProductDto>();
            if (authCheck != null) return authCheck;

            var validation = ValidateModelWithResponse<ProductDto>();
            if (validation != null) return validation;

            var userId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            // Check if user can modify this specific product
            if (!await _productService.CanUserModifyProductAsync(id, userId, userPermissions))
            {
                LogSecurityEvent("Unauthorized Product Update", $"User {userId} attempted to update product {id}");
                return ForbiddenResponse<ProductDto>("You don't have permission to modify this product");
            }

            LogUserAction("Update Product", new { id, userId, request });

            var result = await _productService.UpdateProductAsync(id, request, userId);
            return BuildApiResponse(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteProduct(int id)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            // Check if user can modify this specific product
            if (!await _productService.CanUserModifyProductAsync(id, userId, userPermissions))
            {
                LogSecurityEvent("Unauthorized Product Delete", $"User {userId} attempted to delete product {id}");
                return ForbiddenResponse<string>("You don't have permission to delete this product");
            }

            LogUserAction("Delete Product", new { id, userId });

            var result = await _productService.DeleteProductAsync(id, userId);
            return BuildApiResponse(result);
        }

        [HttpPatch("bulk-update")]
        public async Task<ActionResult<ApiResponse<string>>> BulkUpdateProducts([FromBody] BulkUpdateProductsRequest request)
        {
            var authCheck = CheckAuthenticationWithResponse<string>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<string>(Permission.Permissions.ProductBulkOps);
            if (permissionCheck != null) return permissionCheck;

            var validation = ValidateModelWithResponse<string>();
            if (validation != null) return validation;

            var userId = GetCurrentUserId();

            LogUserAction("Bulk Update Products", new { ProductCount = request.ProductIds.Length, userId, request });

            var result = await _productService.BulkUpdateProductsAsync(request, userId);
            return BuildApiResponse(result);
        }

        [HttpGet("my-products")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetMyProducts()
        {
            var authCheck = CheckAuthenticationWithResponse<IEnumerable<ProductDto>>();
            if (authCheck != null) return authCheck;

            var userId = GetCurrentUserId();

            LogUserAction("Get My Products", new { userId });

            var result = await _productService.GetProductsByCreatorAsync(userId);
            return BuildApiResponse(result);
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetProductStatistics()
        {
            var authCheck = CheckAuthenticationWithResponse<object>();
            if (authCheck != null) return authCheck;

            var permissionCheck = CheckPermissionWithResponse<object>(Permission.Permissions.ViewAnalytics);
            if (permissionCheck != null) return permissionCheck;

            LogUserAction("Get Product Statistics");

            var result = await _productService.GetProductStatisticsAsync();
            return BuildApiResponse(result);
        }
    }