using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class BlacklistedToken
{
    public int Id { get; set; }
        
    [Required]
    public string TokenId { get; set; } = string.Empty; 
        
    [Required]
    public DateTime ExpiryDate { get; set; }
        
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
        
    public string? Reason { get; set; } 
        
    public int? UserId { get; set; } 
    public string? IpAddress { get; set; }
}