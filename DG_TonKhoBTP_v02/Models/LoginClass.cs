using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }

        // role_name -> description
        public Dictionary<string, string> RolesDict { get; set; }

        // role_name -> set(permission_code)
        public Dictionary<string, HashSet<string>> PermissionsDict { get; set; }

        public LoginResult()
        {
            Success = false;
            Message = "";
            Name = "";

            RolesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PermissionsDict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
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
        public static string Name { get; private set; }

        // role_name -> description
        public static Dictionary<string, string> RolesDict { get; private set; }

        // role_name -> set(permission_code)
        public static Dictionary<string, HashSet<string>> PermissionsDict { get; private set; }

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

            RolesDict = login.RolesDict ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PermissionsDict = login.PermissionsDict ?? new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Huỷ session (logout)
        /// </summary>
        public static void Clear()
        {
            IsAuthenticated = false;
            UserId = 0;
            Name = null;

            RolesDict = null;
            PermissionsDict = null;
        }

        /// <summary>
        /// Check quyền theo permission_code (tìm trong tất cả role)
        /// </summary>
        public static bool HasPermission(string permissionCode)
        {
            if (!IsAuthenticated || PermissionsDict == null || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            foreach (var set in PermissionsDict.Values)
            {
                if (set != null && set.Contains(permissionCode))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check quyền theo permission_code trong 1 role cụ thể
        /// </summary>
        public static bool HasPermission(string roleName, string permissionCode)
        {
            if (!IsAuthenticated || PermissionsDict == null ||
                string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return PermissionsDict.TryGetValue(roleName, out var set) &&
                   set != null &&
                   set.Contains(permissionCode);
        }

        /// <summary>
        /// Lấy toàn bộ permission_code của user (gộp từ mọi role, không trùng)
        /// </summary>
        public static HashSet<string> GetAllPermissions()
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!IsAuthenticated || PermissionsDict == null)
                return all;

            foreach (var set in PermissionsDict.Values)
            {
                if (set == null) continue;
                foreach (var p in set) all.Add(p);
            }

            return all;
        }

        /// <summary>
        /// Check role theo role_name
        /// </summary>
        public static bool HasRole(string roleName)
        {
            if (!IsAuthenticated || RolesDict == null || string.IsNullOrWhiteSpace(roleName))
                return false;

            return RolesDict.ContainsKey(roleName);
        }

        /// <summary>
        /// Lấy description của role theo role_name
        /// </summary>
        public static string GetRoleDescription(string roleName)
        {
            if (!IsAuthenticated || RolesDict == null || string.IsNullOrWhiteSpace(roleName))
                return null;

            return RolesDict.TryGetValue(roleName, out var desc) ? desc : null;
        }
    }
}
