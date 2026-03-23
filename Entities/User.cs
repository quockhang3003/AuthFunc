using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class User
{
    public int Id { get; set; }

    [Required] [StringLength(100)] public string UserName { get; set; } = string.Empty;
    [Required] [StringLength(100)][EmailAddress] public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public long Permissions { get; set; } = (long)Enums.Permission.Permissions.None;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int TokenVersion { get; set; } = 0;
    public string? WindowsIdentity { get; set; }
    public string? Domain { get; set; }
    public AuthenticationType AuthType { get; set; } = AuthenticationType.JWT;
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    
    
    public bool HasPermission(Permission.Permissions permission)
    {
        return (Permissions & (long)permission) == (long)permission;
    }
        
    public void GrantPermission(Permission.Permissions permission)
    {
        Permissions |= (long)permission;
    }
        
    public void RevokePermission(Permission.Permissions permission)
    {
        Permissions &= ~(long)permission;
    }
}

