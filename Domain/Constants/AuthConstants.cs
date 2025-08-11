namespace Domain.Constants;

public class AuthConstants
{
    public const string JWT_SCHEME = "JwtBearer";
    public const string WINDOWS_SCHEME = "Windows";
    public const string DEFAULT_SCHEME = JWT_SCHEME;
        
    public const string PERMISSION_CLAIM_TYPE = "permissions";
    public const string AUTH_TYPE_CLAIM_TYPE = "auth_type";
    public const string TOKEN_VERSION_CLAIM_TYPE = "token_version";
        
    public const string WINDOWS_IDENTITY_CLAIM_TYPE = "windows_identity";
    public const string DOMAIN_CLAIM_TYPE = "domain";
}