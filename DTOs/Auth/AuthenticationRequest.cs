using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.DTOs.Auth;

public class AuthenticationRequest
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public AuthenticationType AuthType { get; set; } = AuthenticationType.JWT;
        public string? DeviceInfo { get; set; }
    }

    public class WindowsLoginRequest
    {
        public string? DeviceInfo { get; set; }
        // Windows identity will be extracted from HttpContext
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
        
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        public string? DeviceInfo { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
    }

    public class RevokeTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}