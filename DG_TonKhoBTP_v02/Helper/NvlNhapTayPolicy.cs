using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Chứa duy nhất quy tắc nhận diện dòng NVL/BTP phải nhập tay
    /// KL còn lại và CD còn lại.
    ///
    /// Muốn mở rộng, chỉ cần bổ sung RegexTheoCongDoan bên dưới.
    /// UC_TTNVL và Validator đều dùng chung class này.
    /// </summary>
    public static class NvlNhapTayPolicy
    {
        private static readonly Dictionary<int, string[]> RegexTheoCongDoan
            = new Dictionary<int, string[]>
            {
                // Bổ sung quy tắc tại đây.
                // Ví dụ:
                // {
                //     11,
                //     new[]
                //     {
                //         @"^C\s+123.*$",
                //         @"^C\s+dfsf.*$"
                //     }
                // }
            };

        public static bool ApDung(CongDoan congDoan, TTNVLRow nvl)
        {
            return ApDung(congDoan, nvl?.TenNVL);
        }

        public static bool ApDung(CongDoan congDoan, string tenNVL)
        {
            if (congDoan == null || string.IsNullOrWhiteSpace(tenNVL))
                return false;

            // Công đoạn hàn nối luôn giữ quy tắc hiện tại: KL/CD còn lại bằng 0.
            if (congDoan.Id == 9)
                return false;

            if (!RegexTheoCongDoan.TryGetValue(congDoan.Id, out string[] patterns) ||
                patterns == null || patterns.Length == 0)
            {
                return false;
            }

            string value = tenNVL.Trim();

            foreach (string pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                if (Regex.IsMatch(
                    value,
                    pattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
