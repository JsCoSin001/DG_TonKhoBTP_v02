using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Models
{
    public class PrinterModel
    {
        public string? id { get; set; }
        public string NgaySX { get; set; }
        public string CaSX { get; set; }
        public string? Mau {  get; set; }
        public string? KhoiLuong { get; set; }
        public string? ChieuDai { get; set; }
        // Chỉ tem tạm LOT mới bật cờ này; các tem hiện tại giữ cách in cũ.
        public bool AnDonViKhiGiaTriRong { get; set; } = false;
        public string TenSP { get; set; }
        public string QC { get; set; } = "";
        public string MaBin { get; set; }
        // Giá trị LOT hiển thị trên tem. Null/rỗng => PrintHelper giữ hành vi cũ và dùng MaBin.
        public string Lot { get; set; }
        public string MaSP { get; set; }
        public string DanhGia { get; set; }
        public string TenCN { get; set; }
        public string GhiChu { get; set; }

        public PrinterModel() { }

        public PrinterModel(PrinterModel other)
        {
            id = other.id;
            NgaySX = other.NgaySX;
            CaSX = other.CaSX;
            Mau = other.Mau;
            KhoiLuong = other.KhoiLuong;
            ChieuDai = other.ChieuDai;
            AnDonViKhiGiaTriRong = other.AnDonViKhiGiaTriRong;
            TenSP = other.TenSP;
            QC = other.QC;
            MaBin = other.MaBin;
            Lot = other.Lot;
            MaSP = other.MaSP;
            DanhGia = other.DanhGia;
            TenCN = other.TenCN;
            GhiChu = other.GhiChu;
        }

    }
}
