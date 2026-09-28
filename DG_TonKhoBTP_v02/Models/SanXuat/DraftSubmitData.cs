#nullable enable

using DG_TonKhoBTP_v02.Core;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    /// <summary>
    /// Snapshot dùng riêng cho LƯU TẠM.
    /// TTThanhPham và TTNVL vẫn phải hợp lệ trước khi tạo đối tượng này;
    /// các section khác có thể chưa hoàn chỉnh và được phép chứa null.
    /// </summary>
    internal sealed class DraftSubmitData
    {
        /// <summary>0 = draft mới; > 0 = cập nhật draft đã nạp qua cbxTimQr.</summary>
        public long DraftId { get; set; }

        public int CongDoanId { get; set; }

        public ThongTinCaLamViec? ThongTinCaLamViec { get; set; }

        public TTThanhPham ThongTinThanhPham { get; set; } = new TTThanhPham();

        public List<TTNVLRow> NguyenVatLieuRows { get; set; } = new List<TTNVLRow>();

        public List<TTNVL> NguyenVatLieu { get; set; } = new List<TTNVL>();

        /// <summary>
        /// Có thể null khi người dùng chưa nhập bất kỳ dữ liệu chi tiết công đoạn nào.
        /// </summary>
        public SubmitCongDoanData? CongDoan { get; set; }

        /// <summary>
        /// Dữ liệu dừng máy do người dùng nhập phải được giữ cùng draft.
        /// </summary>
        public List<DanhSachLoiDungMay_Model> DanhSachLoiDungMay { get; set; } =
            new List<DanhSachLoiDungMay_Model>();

        public bool ShouldPrintThanhPham { get; set; }
        public bool ShouldPrintNguyenVatLieu { get; set; }

        public bool IsUpdate => DraftId > 0;
    }
}
