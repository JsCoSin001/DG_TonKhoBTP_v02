using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class ThongTinSPTrongCaLamViec_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("BinNVL")]
        public string BinNVL { get; set; }

        [Column("SoLuongNVL")]
        [Required]
        public double SoLuongNVL { get; set; }

        [Column("SoLuongNVLConLai")]
        [Required]
        public double SoLuongNVLConLai { get; set; }

        [Column("DonViNVL")]
        public string DonViNVL { get; set; }

        [Column("MaBinTP")]
        public string MaBinTP { get; set; }

        [Column("SoLuongTP")]
        [Required]
        public double SoLuongTP { get; set; }

        [Column("DonViTP")]
        [Required]
        public bool DonViTP { get; set; }

        [Column("Phe")]
        [Required]
        public double Phe { get; set; }

        [Column("GhiChu")]
        public string? GhiChu { get; set; }

        [Column("ThongTinCaLamViec_ID")]
        [Required]
        [ForeignKey(nameof(ThongTinCaLamViec))]
        public int ThongTinCaLamViecID { get; set; }

        [Column("DanhSachSP_ID")]
        [Required]
        [ForeignKey(nameof(DanhSachMaSP))]
        public int DanhSachSPID { get; set; }

        [Column("DateInsert")]
        public DateTime? DateInsert { get; set; } = DateTime.Now;

        public virtual ThongTinCaLamViec_Model? ThongTinCaLamViec { get; set; }

        public virtual DanhSachMaSP_Model? DanhSachMaSP { get; set; }

        public virtual ICollection<CDKeoRut_Model> CDKeoRuts { get; set; } = new List<CDKeoRut_Model>();

        public virtual ICollection<CDBenRuotDan_Model> CDBenRuotDans { get; set; } = new List<CDBenRuotDan_Model>();

        public virtual ICollection<CDGhepLoiQuanBang_Model> CDGhepLoiQuanBangs { get; set; } = new List<CDGhepLoiQuanBang_Model>();

        public virtual ICollection<CaiDatCDBoc_Model> CaiDatCDBocs { get; set; } = new List<CaiDatCDBoc_Model>();

    }
}
