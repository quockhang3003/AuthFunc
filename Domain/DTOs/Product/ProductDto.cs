using System.ComponentModel.DataAnnotations;
using Domain.DTOs.Common;

namespace Domain.DTOs.Product;

public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string[]? Tags { get; set; }
        public int ViewCount { get; set; }
    }

    public class CreateProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool IsActive { get; set; } = true;
        public string[]? Tags { get; set; }
    }

    public class UpdateProductRequest
    {
        [StringLength(200, MinimumLength = 1)]
        public string? Name { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }
        
        [Range(0, int.MaxValue)]
        public int? Stock { get; set; }
        
        public bool? IsActive { get; set; }
        public string[]? Tags { get; set; }
    }

    public class ProductSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
        public string[]? Tags { get; set; }
        public string? SortBy { get; set; } = "Id";
        public bool SortDescending { get; set; } = false;
        public bool IncludeInactive { get; set; } = false;
    }

    public class BulkUpdateProductsRequest
    {
        [Required]
        public int[] ProductIds { get; set; } = Array.Empty<int>();
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
        public bool? IsActive { get; set; }
        public string[]? Tags { get; set; }
    }