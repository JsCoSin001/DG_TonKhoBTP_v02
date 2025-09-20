using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("ThongTinSP")]
    public class ThongTinSP
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("DanhSachSP_ID")]
        [Required]
        public int DanhSachSPID { get; set; }

    [Column("ThongTinCaLamViec_ID")]
        [Required]
        public int ThongTinCaLamViecID { get; set; }

    [Column("MaBin")]
        public string MaBin { get; set; }

    [Column("KhoiLuongTruoc")]
        [Required]
        public double KhoiLuongTruoc { get; set; }

    [Column("KhoiLuongSau")]
        [Required]
        public double KhoiLuongSau { get; set; }

    [Column("ChieuDaiTruoc")]
        [Required]
        public double ChieuDaiTruoc { get; set; }

    [Column("ChieuDaiSau")]
        [Required]
        public double ChieuDaiSau { get; set; }

    [Column("Phe")]
        [Required]
        public double Phe { get; set; }

    [Column("GhiChu")]
        public string GhiChu { get; set; }

    [Column("DateInsert")]
        [DataType(DataType.DateTime)]
        public DateTime? DateInsert { get; set; }
    }
}
