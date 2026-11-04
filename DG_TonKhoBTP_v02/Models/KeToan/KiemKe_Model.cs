using System;

namespace DG_TonKhoBTP_v02.Models.KeToan
{
    // Model kiểm kê được dùng thống nhất bởi UC_KiemKe và KiemKe_DB.
    // Không sử dụng DG_TonKhoBTP_v02.Models.KiemKe trong nghiệp vụ này.
    public class KiemKe
    {
        public long? id { get; set; }
        public long? TTThanhPham_ID { get; set; }

        // ID sản phẩm người kiểm kê chọn, lưu riêng tại TTKiemKeThang.
        // Phiếu kiểm kê cũ có thể không có giá trị này.
        public long? DanhSachSP_ID { get; set; }

        public string MaBin { get; set; }
        public decimal? ChieuDai { get; set; }
        public decimal? KhoiLuong { get; set; }
        public string GhiChu { get; set; }
        public string ThoiGianKiemKe { get; set; } = DateTime.Now.ToString("yyyy-MM");
        public string? ApprovedDate { get; set; }
        public string DateInsert { get; set; }
        public string? NguoiKK { get; set; }

        // Dùng để hiển thị trên lưới; không ghi vào TTKiemKeThang.
        public string Ten { get; set; }
    }

    // Model danh mục Bin cho UC_KiemKe và KiemKe_DB.
    public class DanhSachBin
    {
        public int Id { get; set; }
        public string TenBin { get; set; }
        public decimal KhoiLuongBin { get; set; }
    }
}
