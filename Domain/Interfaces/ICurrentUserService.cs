using Domain.Enums;

namespace Domain.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    long Permissions { get; }
    AuthenticationType AuthType { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
        
    bool HasPermission(Permission.Permissions permission);
    bool HasAllPermissions(params Permission.Permissions[] permissions);
    bool HasAnyPermission(params Permission.Permissions[] permissions);
    string? GetClaimValue(string claimType);
    Task<bool> ValidateCurrentTokenAsync();
}