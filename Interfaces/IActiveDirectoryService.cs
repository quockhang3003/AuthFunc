using Domain.DTOs.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IActiveDirectoryService
    {
        Task<AdAuthenticationResult> AuthenticateUserAsync(string username, string password);
        Task<AdUserInfo?> GetUserInfoAsync(string username);
        Task<List<AdGroupInfo>> GetUserGroupsAsync(string username);
        Task<bool> ValidateUserAsync(string username);
        Task<long> MapGroupsToPermissionsAsync(List<string> groups);
        Task<bool> IsUserInGroupAsync(string username, string groupName);
    }
}
