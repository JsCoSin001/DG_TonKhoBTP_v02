using DG_TonKhoBTP_v02.Core;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.SanXuat
{
    /// <summary>
    /// Dữ liệu đã được capture và validate trước khi chuyển sang worker thread.
    /// </summary>
    internal sealed class SubmitFormData
    {
        public int IdEdit { get; set; }
        public string ConfirmedUsername { get; set; }
        public int CongDoanId { get; set; }
        public ThongTinCaLamViec ThongTinCaLamViec { get; set; }
        public TTThanhPham ThongTinThanhPham { get; set; }
        public List<TTNVLRow> NguyenVatLieuRows { get; set; }
        public List<TTNVL> NguyenVatLieu { get; set; }
        public List<string> DanhSachLoiNhapLieu { get; set; } = new List<string>();
        public SubmitCongDoanData CongDoan { get; set; }
        public bool ShouldPrintThanhPham { get; set; }
        public bool ShouldPrintNguyenVatLieu { get; set; }
    }

    /// <summary>
    /// Thay thế cấu trúc List&lt;object&gt; dùng index [0], [1] của chi tiết công đoạn.
    /// </summary>
    internal sealed class SubmitCongDoanData
    {
        public object ChiTietCongDoan { get; set; }
        public CaiDatCDBoc CaiDatCDBoc { get; set; }
    }

    /// <summary>
    /// Kết quả của phần lưu dữ liệu và in tem chạy trên worker thread.
    /// </summary>
    internal sealed class SubmitProcessResult
    {
        public bool SaveSuccess { get; set; }
        public bool HasPrintError { get; set; }
        public string SaveError { get; set; }
        public string PrintError { get; set; }
    }
}