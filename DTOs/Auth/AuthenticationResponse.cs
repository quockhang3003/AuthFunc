using Domain.DTOs.User;
using Domain.Enums;

namespace Domain.DTOs.Auth;

public class AuthenticationResponse
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
        public AuthenticationType AuthType { get; set; }
        public string[] GrantedPermissions { get; set; } = Array.Empty<string>();
    }

    public class TokenValidationResponse
    {
        public bool IsValid { get; set; }
        public int UserId { get; set; }
        public long Permissions { get; set; }
        public AuthenticationType AuthType { get; set; }
        public int TokenVersion { get; set; }
        public DateTime ExpiryTime { get; set; }
        public string? ErrorMessage { get; set; }
    }
}