using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDBenRuotDan_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("DuongKinh")]
        [Required]
        public double DuongKinh { get; set; }

        [Column("KetCauSP")]
        public string KetCauSP { get; set; }

        [Column("Chiều Xoắn")]
        [Required]
        public bool ChieuXoan { get; set; }

        [Column("BuocBen")]
        [Required]
        public double BuocBen { get; set; }

        [Column("TrongLuong")]
        [Required]
        public double TrongLuong { get; set; }

        [Column("ThongTinSPTrongCaLamViec_ID")]
        [Required]
        [ForeignKey(nameof(ThongTinSPTrongCaLamViec))]
        public int ThongTinSPTrongCaLamViecID { get; set; }

        public virtual ThongTinSPTrongCaLamViec_Model? ThongTinSPTrongCaLamViec { get; set; }

    }
}
