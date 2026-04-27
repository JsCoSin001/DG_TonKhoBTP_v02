using System;
using System.Data.SQLite;

namespace DG_TonKhoBTP_v02.Database
{
    internal class User_DatabaseHelper
    {
        public static bool UpdatePassword(int userId, string newPassword)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId không hợp lệ.");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Mật khẩu mới không được để trống.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            using (var conn = new SQLiteConnection(DatabaseHelper.GetStringConnector))
            {
                conn.Open();

                string sql = @"
                    UPDATE users
                    SET password_hash = @password_hash
                    WHERE user_id = @user_id
                      AND is_active = 1;
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    cmd.Parameters.AddWithValue("@user_id", userId);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}