using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Nhận diện dòng NVL/BTP phải nhập tay KL còn lại và CD còn lại,
    /// chỉ dựa trên TenNVL.
    /// </summary>
    public static class NvlNhapTayPolicy
    {
        // Nhóm điều kiện dương: chỉ cần khớp ít nhất một biểu thức (OR).
        private static readonly string[] RegexNhapTay =
        {
            @"^C .*R.*$",
            @"^C-AWG .*$",
            @"^C 1\.02$",
            @"^C 1\.20$",
            @"^A .*R.*$"
        };

        // Điều kiện loại trừ: TenNVL kết thúc bằng /T.
        private static readonly string RegexLoaiTru = @"/T$";

        public static bool ApDung(TTNVLRow nvl)
        {
            return ApDung(nvl?.TenNVL);
        }

        public static bool ApDung(string tenNVL)
        {

            if (string.IsNullOrWhiteSpace(tenNVL))
                return false;

            string value = tenNVL.Trim();
            const RegexOptions options =
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

            // AND NOT: nếu khớp điều kiện loại trừ thì không áp dụng nhập tay.
            if (Regex.IsMatch(value, RegexLoaiTru, options))
                return false;

            // OR: chỉ cần khớp ít nhất một điều kiện dương.
            return RegexNhapTay.Any(pattern =>
                !string.IsNullOrWhiteSpace(pattern) &&
                Regex.IsMatch(value, pattern, options));
        }
    }
}