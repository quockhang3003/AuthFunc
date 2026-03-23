using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Product
{
    public int Id { get; set; }

    [Required] [StringLength(200)] public string Name { get; set; } = string.Empty;
    [Required] [StringLength(1000)] public string Description { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Price { get; set; }
    [Range(0, double.MaxValue)] public int Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public int CreatedBy { get; set; }
    public virtual User Creator { get; set; } = null!;
        
    public int? UpdatedBy { get; set; }
    public string? Tags { get; set; } 
    public int ViewCount { get; set; } = 0;
}