namespace Domain.Enums;

public class Permission
{
    [Flags]
    public enum Permissions : long
    {
        None = 0,
        
        // User Management (1-32)
        ViewUsers = 1L << 0,           
        CreateUsers = 1L << 1,         
        UpdateUsers = 1L << 2,         
        DeleteUsers = 1L << 3,         
        ManageUserRoles = 1L << 4,     
        ViewUserDetails = 1L << 5,     
        
        // Product Management (64-4096)
        ViewProducts = 1L << 6,        
        CreateProducts = 1L << 7,      
        UpdateProducts = 1L << 8,      
        DeleteProducts = 1L << 9,      
        ManageProductCategories = 1L << 10, 
        ViewProductDetails = 1L << 11, 
        ProductBulkOps = 1L << 12,  
        
        // System Administration (8192-65536)
        ViewSystemLogs = 1L << 13,     
        ManageSystem = 1L << 14,       
        BackupRestore = 1L << 15,     
        ManagePermissions = 1L << 16,  
        
        // Reports & Analytics (131072-524288)
        ViewReports = 1L << 17,        
        GenerateReports = 1L << 18,    
        ViewAnalytics = 1L << 19,     
        
        BasicUser = ViewProducts | ViewProductDetails,
        ProductManager = ViewProducts | CreateProducts | UpdateProducts | ViewProductDetails | ManageProductCategories|ProductBulkOps,
        UserManager = ViewUsers | CreateUsers | UpdateUsers | ManageUserRoles | ViewUserDetails,
        SystemAdmin = ViewSystemLogs | ManageSystem | BackupRestore | ManagePermissions,
        Administrator = ViewUsers | CreateUsers | UpdateUsers | DeleteUsers | ManageUserRoles | ViewUserDetails |
                        ViewProducts | CreateProducts | UpdateProducts | DeleteProducts | ManageProductCategories | ViewProductDetails |
                        ViewSystemLogs | ManageSystem | BackupRestore | ManagePermissions |
                        ViewReports | GenerateReports | ViewAnalytics
    }
}
public enum AuthenticationType
{
    JWT = 1,
    Windows = 2,
    Hybrid = 3
}

public enum TokenType
{
    AccessToken = 1,
    RefreshToken = 2
}