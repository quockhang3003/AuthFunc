using Domain.DTOs.ActiveDirectory;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implements
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly string _domain;
        private readonly string _ldapPath;
        private readonly Dictionary<string, string> _groupMappings;

        public ActiveDirectoryService(
            IConfiguration configuration,
            ILogger<ActiveDirectoryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _domain = _configuration["ActiveDirectory:Domain"] ?? throw new InvalidOperationException("AD Domain not configured");
            _ldapPath = _configuration["ActiveDirectory:LdapPath"] ?? throw new InvalidOperationException("LDAP Path not configured");

            // Load group mappings from config
            _groupMappings = _configuration.GetSection("ActiveDirectory:GroupMappings")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
        }

        public async Task<AdAuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain, _domain);

                    // Validate credentials
                    var isValid = context.ValidateCredentials(username, password);

                    if (!isValid)
                    {
                        return new AdAuthenticationResult
                        {
                            IsAuthenticated = false,
                            ErrorMessage = "Invalid username or password"
                        };
                    }

                    // Get user info
                    var userInfo = GetUserInfoSync(username);

                    if (userInfo == null)
                    {
                        return new AdAuthenticationResult
                        {
                            IsAuthenticated = false,
                            ErrorMessage = "User not found in Active Directory"
                        };
                    }

                    if (!userInfo.IsEnabled)
                    {
                        return new AdAuthenticationResult
                        {
                            IsAuthenticated = false,
                            ErrorMessage = "User account is disabled"
                        };
                    }

                    if (userInfo.IsLockedOut)
                    {
                        return new AdAuthenticationResult
                        {
                            IsAuthenticated = false,
                            ErrorMessage = "User account is locked out"
                        };
                    }

                    return new AdAuthenticationResult
                    {
                        IsAuthenticated = true,
                        UserInfo = userInfo
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error authenticating user {Username} with AD", username);
                    return new AdAuthenticationResult
                    {
                        IsAuthenticated = false,
                        ErrorMessage = $"Authentication error: {ex.Message}"
                    };
                }
            });
        }

        public async Task<AdUserInfo?> GetUserInfoAsync(string username)
        {
            return await Task.Run(() => GetUserInfoSync(username));
        }

        private AdUserInfo? GetUserInfoSync(string username)
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain, _domain);
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user == null)
                {
                    _logger.LogWarning("User {Username} not found in AD", username);
                    return null;
                }

                // Get additional properties using DirectoryEntry
                var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;

                var userInfo = new AdUserInfo
                {
                    Username = user.SamAccountName ?? username,
                    DisplayName = user.DisplayName ?? username,
                    Email = user.EmailAddress ?? $"{username}@{_domain}",
                    Domain = _domain,
                    DistinguishedName = user.DistinguishedName ?? "",
                    IsEnabled = user.Enabled ?? false,
                    IsLockedOut = user.IsAccountLockedOut(),
                    AccountExpirationDate = user.AccountExpirationDate,
                    LastLogon = user.LastLogon
                };

                // Get additional properties from DirectoryEntry
                if (directoryEntry != null)
                {
                    userInfo.Department = GetProperty(directoryEntry, "department");
                    userInfo.Title = GetProperty(directoryEntry, "title");
                }

                // Get user groups
                userInfo.Groups = GetUserGroupsSync(user);

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info for {Username}", username);
                return null;
            }
        }

        public async Task<List<AdGroupInfo>> GetUserGroupsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain, _domain);
                    using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                    if (user == null)
                        return new List<AdGroupInfo>();

                    var groups = user.GetAuthorizationGroups()
                        .Cast<GroupPrincipal>()
                        .Select(g => new AdGroupInfo
                        {
                            Name = g.Name ?? "",
                            DistinguishedName = g.DistinguishedName ?? "",
                            Description = g.Description ?? ""
                        })
                        .ToList();

                    return groups;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting groups for user {Username}", username);
                    return new List<AdGroupInfo>();
                }
            });
        }

        private List<string> GetUserGroupsSync(UserPrincipal user)
        {
            try
            {
                return user.GetAuthorizationGroups()
                    .Cast<GroupPrincipal>()
                    .Select(g => g.Name ?? "")
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                return new List<string>();
            }
        }

        public async Task<bool> ValidateUserAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain, _domain);
                    using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                    if (user == null)
                        return false;

                    // Check if account is enabled and not locked
                    return (user.Enabled ?? false) && !user.IsAccountLockedOut();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating user {Username}", username);
                    return false;
                }
            });
        }

        public async Task<long> MapGroupsToPermissionsAsync(List<string> groups)
        {
            return await Task.Run(() =>
            {
                long permissions = (long)Permission.Permissions.None;

                foreach (var group in groups)
                {
                    if (_groupMappings.TryGetValue(group, out var permissionNames))
                    {
                        var perms = permissionNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var permName in perms)
                        {
                            if (Enum.TryParse<Permission.Permissions>(permName.Trim(), out var permission))
                            {
                                permissions |= (long)permission;
                            }
                        }
                    }
                }

                // If no permissions mapped, give basic user permission
                if (permissions == (long)Permission.Permissions.None)
                {
                    permissions = (long)Permission.Permissions.BasicUser;
                }

                _logger.LogInformation("Mapped groups {Groups} to permissions: {Permissions}",
                    string.Join(", ", groups), permissions);

                return permissions;
            });
        }

        public async Task<bool> IsUserInGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain, _domain);
                    using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                    if (user == null)
                        return false;

                    var groups = user.GetAuthorizationGroups()
                        .Cast<GroupPrincipal>()
                        .Select(g => g.Name ?? "")
                        .ToList();

                    return groups.Contains(groupName, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking group membership for {Username}", username);
                    return false;
                }
            });
        }

        private string GetProperty(DirectoryEntry entry, string propertyName)
        {
            try
            {
                if (entry.Properties.Contains(propertyName))
                {
                    var value = entry.Properties[propertyName].Value;
                    return value?.ToString() ?? "";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
