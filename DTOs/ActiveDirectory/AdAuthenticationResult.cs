using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.ActiveDirectory
{
    public class AdAuthenticationResult
    {
        public bool IsAuthenticated { get; set; }
        public AdUserInfo? UserInfo { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
