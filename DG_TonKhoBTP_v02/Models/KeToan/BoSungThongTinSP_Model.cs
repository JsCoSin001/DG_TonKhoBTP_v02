namespace DG_TonKhoBTP_v02.Models.KeToan
{
    /// <summary>
    /// Model ánh xạ bảng TTBoSung trong SQLite.
    /// </summary>
    public class BoSungThongTinSP_Model
    {
        public long Id { get; set; }

        /// <summary>FK → DanhSachMaSP.id</summary>
        public long DanhSachMaSP_ID { get; set; }

        /// <summary>Lấy từ DanhSachMaSP.Ma — chỉ dùng để hiển thị, không lưu vào TTBoSung.</summary>
        public string Ma { get; set; }

        /// <summary>Lấy từ DanhSachMaSP.Ten — chỉ dùng để hiển thị, không lưu vào TTBoSung.</summary>
        public string Ten { get; set; }

        public string TenChiTiet { get; set; }
        public string TieuChuan { get; set; }

        // ── Các thông số kỹ thuật ──────────────────────────────────────────
        public string Rph { get; set; }
        public string Rt { get; set; }
        public string Rtd { get; set; }
        public string Rcd { get; set; }
        public string Ca { get; set; }
        public string Is { get; set; }
        public string Istt { get; set; }
        public string Sh { get; set; }
        public string No { get; set; }
        public string D { get; set; }
        public string TNC { get; set; }
        public string OD { get; set; }
        public string T { get; set; }

        /// <summary>Cột M trong DB (tbMau trên UI)</summary>
        public string M { get; set; }

        /// <summary>Cột KC trong DB (tbKetCau trên UI)</summary>
        public string KC { get; set; }

        /// <summary>Cột LTDN trong DB (tbLtdn trên UI)</summary>
        public string LTDN { get; set; }

        public string GhiChu { get; set; }
        public string LH { get; set; }
    }
}