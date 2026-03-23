using System.ComponentModel.DataAnnotations;
using Domain.DTOs.Common;
using Domain.Enums;

namespace Domain.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public long Permissions { get; set; }
    public string[] PermissionNames { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public AuthenticationType AuthType { get; set; }
    public string? Domain { get; set; }
    public int ActiveSessionCount { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class CreateUserRequest
{
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; } // Nullable for Windows auth
        
        public long Permissions { get; set; } = (long)Enums.Permission.Permissions.ViewUsers;
        public AuthenticationType AuthType { get; set; } = AuthenticationType.JWT;
        public string? WindowsIdentity { get; set; }
        public string? Domain { get; set; }
}

public class UpdateUserRequest
{
    [StringLength(100, MinimumLength = 3)]
    public string? Username { get; set; }
        
    [EmailAddress]
    public string? Email { get; set; }
        
    public bool? IsActive { get; set; }
    public long? Permissions { get; set; }
}

public class ChangePermissionsRequest
{
    [Required]
    public long Permissions { get; set; }
    public string? Reason { get; set; }
}

public class UserSearchRequest : PaginationRequest
{
    public string? Search { get; set; }
    public AuthenticationType? AuthType { get; set; }
    public bool? IsActive { get; set; }
    public long? HasPermission { get; set; }
}

