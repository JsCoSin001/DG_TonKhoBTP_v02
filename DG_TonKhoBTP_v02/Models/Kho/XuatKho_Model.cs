using System;

namespace DG_TonKhoBTP_v02.Models.Kho
{
    /// <summary>
    /// Dữ liệu một dòng trong dgvLayDL (cuộn/dây gốc + giá trị người dùng nhập).
    /// </summary>
    public class XuatKhoRow
    {
        // ── Khoá nội bộ ──────────────────────────────────────────────────────────
        public long TTCuonDay_ID { get; set; }
        public long TTNhapKho_ID { get; set; }

        // ── Dữ liệu gốc từ TTNhapKho / TTCuonDay (readonly) ─────────────────────
        public int TongChieuDai { get; set; }
        public int? SoCuon { get; set; }
        public int? SoDau { get; set; }
        public int? SoCuoi { get; set; }
        public string GhiChu { get; set; } = string.Empty;

        // ── Giá trị người dùng nhập ──────────────────────────────────────────────
        public int? SoCuon_User { get; set; }
        public int? SoDau_User { get; set; }
        public int? SoCuoi_User { get; set; }
        public string GhiChu_User { get; set; } = string.Empty;

        /// <summary>true = checkbox "Lấy tất" được tích.</summary>
        public bool GetAll { get; set; }
    }

    /// <summary>
    /// Dữ liệu một dòng trong dgvLayDL_preview (kết quả tìm kiếm / lưu).
    /// Gộp cả thông tin gốc (từ TTNhapKho/TTCuonDay) lẫn giá trị đã xuất (TTXuatKho).
    /// </summary>
    public class XuatKhoPreviewRow
    {
        // ── Id bản ghi TTXuatKho (ẩn, dùng cho double-click) ────────────────────
        public long Id { get; set; }

        // ── Thông tin chung ──────────────────────────────────────────────────────
        public string TenSP { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string NgayXuat { get; set; } = string.Empty;
        public string NguoiLam { get; set; } = string.Empty;

        // ── Dữ liệu gốc từ TTNhapKho / TTCuonDay ────────────────────────────────
        public int TongChieuDai { get; set; }
        public int? SoCuon { get; set; }
        public int? SoDau { get; set; }
        public int? SoCuoi { get; set; }
        public string GhiChu { get; set; } = string.Empty;

        // ── Giá trị đã xuất từ TTXuatKho ────────────────────────────────────────
        public int? SoCuon_User { get; set; }
        public int? SoDau_User { get; set; }
        public int? SoCuoi_User { get; set; }
        public string GhiChu_User { get; set; } = string.Empty;
    }
}