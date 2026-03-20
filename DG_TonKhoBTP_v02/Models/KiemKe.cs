using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class KiemKe
    {
        public long? id { get; set; }

        // map từ tbMa_KK
        public long? TTThanhPham_ID { get; set; }
        public long? DanhSachSP_ID { get; set; }
        public string? NguoiKK { get; set; }

        // map từ tbQr
        public string MaBin { get; set; }

        // map từ nbrChieuDai_KK
        public decimal? ChieuDai { get; set; }

        // mình map từ nbrKLTong_KK vì bảng chỉ có 1 cột KhoiLuong
        public decimal? KhoiLuong { get; set; }

        // map từ rtbGhiChu
        public string GhiChu { get; set; }

        public string ThoiGianKiemKe { get; set; } = DateTime.Now.ToString("MM/yyyy");
        public string ApprovedDate { get; set; }
        public string DateInsert { get; set; }

        // chỉ dùng để hiển thị grid, không insert DB
        public string Ten { get; set; }
    }
}
