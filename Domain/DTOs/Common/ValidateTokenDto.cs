using Domain.Enums;

namespace Domain.DTOs.Common;

public class ValidateTokenDto
{
    public int UserId { get; set; }
    public long Permissions { get; set; }
    public AuthenticationType AuthType { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsValid { get; set; }
}