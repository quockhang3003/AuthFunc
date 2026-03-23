using Domain.Enums;

namespace Domain.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public AuthenticationType AuthType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessAt { get; set; }
    public bool IsActive { get; set; } = true;
        
    public virtual User User { get; set; } = null!;
}