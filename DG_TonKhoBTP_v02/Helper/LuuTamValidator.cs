using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Validation dành riêng cho Lưu tạm.
    /// Chỉ kiểm tra Thành phẩm và NVL theo đúng rule hiện tại;
    /// không validate ca làm việc/chi tiết công đoạn và không thao tác database.
    /// </summary>
    public static class LuuTamValidator
    {
        public static List<string> LayDanhSachLoi(
            TTThanhPham thanhPham,
            List<TTNVLRow> nvlRows,
            string tenMay,
            CongDoan congDoan,
            bool boQuaNvl)
        {
            var errors = new List<string>();

            errors.AddRange(Validator.LayDanhSachLoiTTThanhPham(thanhPham));

            if (!boQuaNvl)
            {
                errors.AddRange(Validator.LayDanhSachLoiTTNVL(
                    nvlRows,
                    tenMay,
                    congDoan));
            }

            return errors
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
