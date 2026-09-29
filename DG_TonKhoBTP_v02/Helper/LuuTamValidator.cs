using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.Helper
{
    /// <summary>
    /// Validation tối thiểu dành riêng cho Lưu tạm.
    /// Không dùng Validator của Lưu chính thức.
    /// </summary>
    public static class LuuTamValidator
    {
        /// <summary>
        /// Lưu tạm chỉ yêu cầu đã chọn Thành phẩm và có MaBin để quản lý draft.
        /// Không kiểm KL/CD hoặc các trường hoàn thiện khác.
        /// </summary>
        public static List<string> LayDanhSachLoiThanhPham(TTThanhPham thanhPham)
        {
            var errors = new List<string>();

            if (thanhPham == null)
            {
                errors.Add("Không có dữ liệu Thành phẩm.");
                return errors;
            }

            if (thanhPham.DanhSachSP_ID <= 0)
                errors.Add("Chưa chọn Thành phẩm hợp lệ (DanhSachSP_ID <= 0).");

            if (string.IsNullOrWhiteSpace(thanhPham.MaBin))
                errors.Add("MaBin Thành phẩm đang trống.");

            return ChuanHoa(errors);
        }

        /// <summary>
        /// Mặc định Lưu tạm phải có ít nhất một NVL. Công đoạn 9 là ngoại lệ
        /// vì nghiệp vụ hiện tại không sử dụng NVL.
        /// </summary>
        public static List<string> LayDanhSachLoiNguyenVatLieu(
            List<TTNVLRow> nvlRows,
            CongDoan congDoan)
        {
            var errors = new List<string>();

            if (congDoan?.Id == 9)
                return errors;

            if (nvlRows == null || nvlRows.Count == 0)
                errors.Add("Chưa có Nguyên vật liệu.");

            return ChuanHoa(errors);
        }

        private static List<string> ChuanHoa(IEnumerable<string> errors)
        {
            return (errors ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
