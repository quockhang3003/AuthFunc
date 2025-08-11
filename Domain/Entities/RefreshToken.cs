using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    [Required] public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;
        
    public DateTime? RevokedAt { get; set; }
    public string RevokedByIp { get; set; } = string.Empty;
    public string ReplacedByToken { get; set; } = string.Empty;
    public bool IsActive => RevokedAt == null && !IsExpired;
        
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
        
    // Additional tracking
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public AuthenticationType AuthType { get; set; } = AuthenticationType.JWT;
}