using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database.Authentication
{
    public static class Login_DB
    {
        public static LoginResult Login(string usernameInput, string passwordInput)
        {
            LoginResult result = new LoginResult();

            if (string.IsNullOrWhiteSpace(usernameInput) || string.IsNullOrEmpty(passwordInput))
            {
                result.Message = "Tài khoản hoặc mật khẩu đang bỏ trống.";
                return result;
            }

            using (SQLiteConnection conn = DB_Base.OpenConnection())
            {
                int userId = 0;
                string storedHash = null;
                string name = null;
                string username = null;

                string sqlUser = @"
                    SELECT 
                        user_id, 
                        username, 
                        name, 
                        password_hash
                    FROM users
                    WHERE username = @username COLLATE NOCASE
                      AND is_active = 1
                    LIMIT 1;
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(sqlUser, conn))
                {
                    cmd.Parameters.AddWithValue("@username", usernameInput.Trim());

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            result.Message = "Sai tài khoản hoặc mật khẩu.";
                            return result;
                        }

                        userId = Convert.ToInt32(rd["user_id"]);
                        name = rd["name"] == DBNull.Value ? "" : Convert.ToString(rd["name"]);
                        username = rd["username"] == DBNull.Value ? "" : Convert.ToString(rd["username"]);
                        storedHash = rd["password_hash"] == DBNull.Value ? null : Convert.ToString(rd["password_hash"]);
                    }
                }

                if (string.IsNullOrWhiteSpace(storedHash))
                {
                    result.Message = "Sai tài khoản hoặc mật khẩu.";
                    return result;
                }

                bool passwordOk;

                try
                {
                    passwordOk = BCrypt.Net.BCrypt.Verify(passwordInput, storedHash);
                }
                catch
                {
                    passwordOk = false;
                }

                if (!passwordOk)
                {
                    result.Message = "Sai tài khoản hoặc mật khẩu.";
                    return result;
                }

                string sqlPerms = @"
                    SELECT
                        r.role_name,
                        r.description AS role_description,
                        p.permission_code
                    FROM user_roles ur
                    JOIN roles r 
                        ON r.role_id = ur.role_id
                    LEFT JOIN role_permissions rp 
                        ON rp.role_id = r.role_id
                    LEFT JOIN permissions p 
                        ON p.permission_id = rp.permission_id
                    WHERE ur.user_id = @user_id
                    ORDER BY r.role_name, p.permission_code;
                ";

                var rolesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var permsDict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                using (SQLiteCommand cmd = new SQLiteCommand(sqlPerms, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);

                    using (SQLiteDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            if (rd["role_name"] == DBNull.Value)
                                continue;

                            string roleName = Convert.ToString(rd["role_name"]);
                            string roleDesc = rd["role_description"] == DBNull.Value
                                ? null
                                : Convert.ToString(rd["role_description"]);

                            if (string.IsNullOrWhiteSpace(roleName))
                                continue;

                            roleName = roleName.Trim();

                            if (!rolesDict.ContainsKey(roleName))
                            {
                                rolesDict.Add(roleName, roleDesc);
                            }

                            if (!permsDict.TryGetValue(roleName, out HashSet<string> permissionSet))
                            {
                                permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                permsDict[roleName] = permissionSet;
                            }

                            if (rd["permission_code"] != DBNull.Value)
                            {
                                string permissionCode = Convert.ToString(rd["permission_code"]);

                                if (!string.IsNullOrWhiteSpace(permissionCode))
                                {
                                    permissionSet.Add(permissionCode.Trim());
                                }
                            }
                        }
                    }
                }

                result.Success = true;
                result.UserId = userId;
                result.Name = name;
                result.UserName = username;
                result.RolesDict = rolesDict;
                result.PermissionsDict = permsDict;
                result.Message = "OK";

                return result;
            }
        }
    }
}