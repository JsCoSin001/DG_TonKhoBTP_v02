using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }

        public List<string> Roles { get; set; }
        public HashSet<string> Permissions { get; set; }

        public LoginResult()
        {
            Success = false;
            Message = "";
            Name = "";

            Roles = new List<string>();
            Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }



    public class RoleInfo
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
    }


    public class UserInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UserWithRoles
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public List<RoleInfo> Roles { get; set; }
    }


    public static class UserContext
    {
        public static bool IsAuthenticated { get; private set; }

        public static int UserId { get; private set; }
        //public static string Username { get; private set; }
        public static string Name { get; private set; }
        public static List<string> Roles { get; private set; }
        public static HashSet<string> Permissions { get; private set; }

        /// <summary>
        /// Set session khi login thành công
        /// </summary>
        public static void Set(LoginResult login)
        {
            if (login == null || !login.Success)
                throw new InvalidOperationException("LoginResult không hợp lệ.");

            IsAuthenticated = true;
            UserId = login.UserId;
            Name = login.Name;
            Roles = login.Roles;
            Permissions = login.Permissions;
        }

        /// <summary>
        /// Huỷ session (logout)
        /// </summary>
        public static void Clear()
        {
            IsAuthenticated = false;
            UserId = 0;
            Name = null;
            Roles = null;
            Permissions = null;
        }

        /// <summary>
        /// Check quyền nhanh gọn ở mọi nơi
        /// </summary>
        public static bool HasPermission(string permissionCode)
        {
            if (!IsAuthenticated || Permissions == null)
                return false;

            return Permissions.Contains(permissionCode);
        }
    }
}
