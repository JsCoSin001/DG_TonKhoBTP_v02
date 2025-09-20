using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CaiDatCDBoc")]
    public class CaiDatCDBoc
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSPT_ID")]
        [Required]
        public int ThongTinSPTID { get; set; }

    [Column("MangNuoc")]
        [Required]
        public bool MangNuoc { get; set; }

    [Column("PuliDanDay")]
        [Required]
        public bool PuliDanDay { get; set; }

    [Column("BoDemMet")]
        [Required]
        public bool BoDemMet { get; set; }

    [Column("MayIn")]
        public bool? MayIn { get; set; }

    [Column("v1")]
        public double? V1 { get; set; }

    [Column("v2")]
        public double? V2 { get; set; }

    [Column("v3")]
        public double? V3 { get; set; }

    [Column("v4")]
        public double? V4 { get; set; }

    [Column("v5")]
        public double? V5 { get; set; }

    [Column("v6")]
        public double? V6 { get; set; }

    [Column("Co")]
        public double? Co { get; set; }

    [Column("Dau1")]
        public double? Dau1 { get; set; }

    [Column("Dau2")]
        public double? Dau2 { get; set; }

    [Column("Khuon")]
        public double? Khuon { get; set; }

    [Column("BinhSay")]
        public double? BinhSay { get; set; }

    [Column("DKKhuon1")]
        [Required]
        public double DKKhuon1 { get; set; }

    [Column("DKKhuon2")]
        [Required]
        public double DKKhuon2 { get; set; }

    [Column("TTNhua")]
        public string TTNhua { get; set; }

    [Column("NhuaPhe")]
        [Required]
        public double NhuaPhe { get; set; }

    [Column("GhiChuNhuaPhe")]
        public string GhiChuNhuaPhe { get; set; }

    [Column("DayPhe")]
        [Required]
        public double DayPhe { get; set; }

    [Column("GhiChuDayPhe")]
        public string GhiChuDayPhe { get; set; }

    [Column("KTDKLan1")]
        [Required]
        public double KTDKLan1 { get; set; }

    [Column("KTDKLan2")]
        [Required]
        public double KTDKLan2 { get; set; }

    [Column("KTDKLan3")]
        [Required]
        public double KTDKLan3 { get; set; }

    [Column("DiemMongLan1")]
        [Required]
        public double DiemMongLan1 { get; set; }

    [Column("DiemMongLan2")]
        [Required]
        public double DiemMongLan2 { get; set; }
    }
}
