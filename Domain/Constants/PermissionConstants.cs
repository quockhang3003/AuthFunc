using Domain.Enums;

namespace Domain.Constants;

public class PermissionConstants
{
    public const long BASIC_USER_PERMISSIONS = (long)(
        Permission.Permissions.ViewProducts |
        Permission.Permissions.ViewProductDetails
    );

    public const long PRODUCT_MANAGER_PERMISSIONS = (long)(
        Permission.Permissions.ViewProducts |
        Permission.Permissions.CreateProducts |
        Permission.Permissions.UpdateProducts |
        Permission.Permissions.ViewProductDetails |
        Permission.Permissions.ManageProductCategories
    );

    public const long USER_MANAGER_PERMISSIONS = (long)(
        Permission.Permissions.ViewUsers |
        Permission.Permissions.CreateUsers |
        Permission.Permissions.UpdateUsers |
        Permission.Permissions.ManageUserRoles |
        Permission.Permissions.ViewUserDetails
    );

    public const long SYSTEM_ADMIN_PERMISSIONS = (long)(
        Permission.Permissions.ViewSystemLogs |
        Permission.Permissions.ManageSystem |
        Permission.Permissions.BackupRestore |
        Permission.Permissions.ManagePermissions
    );

    public const long ADMINISTRATOR_PERMISSIONS = (long)(
        Permission.Permissions.ViewUsers |
        Permission.Permissions.CreateUsers |
        Permission.Permissions.UpdateUsers |
        Permission.Permissions.DeleteUsers |
        Permission.Permissions.ManageUserRoles |
        Permission.Permissions.ViewUserDetails |
        Permission.Permissions.ViewProducts |
        Permission.Permissions.CreateProducts |
        Permission.Permissions.UpdateProducts |
        Permission.Permissions.DeleteProducts |
        Permission.Permissions.ManageProductCategories |
        Permission.Permissions.ViewProductDetails |
        Permission.Permissions.ViewSystemLogs |
        Permission.Permissions.ManageSystem |
        Permission.Permissions.BackupRestore |
        Permission.Permissions.ManagePermissions |
        Permission.Permissions.ViewReports |
        Permission.Permissions.GenerateReports |
        Permission.Permissions.ViewAnalytics
    );
        
    public const int DEFAULT_PAGE_SIZE = 10;
    public const int MAX_PAGE_SIZE = 100;
    public const int ACCESS_TOKEN_EXPIRY_MINUTES = 15;
    public const int REFRESH_TOKEN_EXPIRY_DAYS = 7;
    public const int MAX_REFRESH_TOKENS_PER_USER = 5;
    public const int TOKEN_CLEANUP_INTERVAL_MINUTES = 30;
}