using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.ActiveDirectory
{
    public class AdUserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public List<string> Groups { get; set; } = new();
        public bool IsEnabled { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? AccountExpirationDate { get; set; }
        public DateTime? LastLogon { get; set; }
    }
}
