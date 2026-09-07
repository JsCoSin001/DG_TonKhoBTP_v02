using DG_TonKhoBTP_v02.Database.SanXuat;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.UI.Helper
{
    /// <summary>
    /// Helper dùng chung để lấy danh sách máy theo MaCongDoan.
    /// Trả về object để các màn hình khác có thể tái sử dụng id, mã công đoạn và tên máy.
    /// </summary>
    internal static class DanhSachMayHelper
    {
        public static List<DanhSachMay_Model> LayTheoMaCongDoan(int maCongDoan)
        {
            return LoiDungMay_DB.GetDanhSachMayTheoMaCongDoan(maCongDoan);
        }
    }
}
