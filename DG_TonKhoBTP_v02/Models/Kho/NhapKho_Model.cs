using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class ThongTinCuonDay
    {
        public int SoCuon { get; set; }
        public int TongChieuDai { get; set; }
        public int SoDau { get; set; }
        public int SoCuoi { get; set; }
        public string Ghichu { get; set; }
    }

    /// <summary>
    /// Ánh xạ với bảng TTNhapKho (schema mới – không còn SoDau/SoCuoi/TongChieuDai/ThongTinCuon).
    /// Danh sách cuộn/lô chi tiết nằm trong List&lt;ThongTinCuonDay&gt; truyền kèm.
    /// </summary>
    public class NhapKho_Model
    {
        /// <summary>id tự sinh – để trống khi INSERT, có giá trị sau khi lưu.</summary>
        public long Id { get; set; }

        /// <summary>Ngày nhập kho, định dạng dd/MM/yyyy.</summary>
        public string Ngay { get; set; }

        /// <summary>Số biên bản.</summary>
        public int SoBB { get; set; }

        /// <summary>FK → TTThanhPham.id</summary>
        public long TTThanhPham_ID { get; set; }

        /// <summary>Tên sản phẩm (denorm để hiển thị nhanh).</summary>
        public string TenSP { get; set; }

        /// <summary>Tổng số mét nhập kho.</summary>
        public double SoMet { get; set; }

        /// <summary>"Hàng đặt" hoặc "Hàng bán".</summary>
        public string LoaiDon { get; set; }

        /// <summary>Tên khách hàng (bắt buộc khi LoaiDon = "Hàng đặt").</summary>
        public string KhachHang { get; set; }

        /// <summary>Ghi chú tự do.</summary>
        public string GhiChu { get; set; }

        /// <summary>"Lô" hoặc "Cuộn".</summary>
        public string Loai { get; set; }

        /// <summary>Chiều cao lô (chỉ dùng khi Loai = "Lô").</summary>
        public double ChieuCaoLo { get; set; }

        /// <summary>Người thực hiện nhập kho.</summary>
        public string NguoiLam { get; set; }

        public string ThongTinCuonDay { get; set; }
    }
}