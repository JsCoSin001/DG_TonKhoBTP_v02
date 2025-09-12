using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDGhepLoiQuanBang_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("DKSoiDong")]
        [Required]
        public double DKSoiDong { get; set; }

        [Column("DKSoiMach")]
        [Required]
        public double DKSoiMach { get; set; }

        [Column("BuocXoan")]
        [Required]
        public double BuocXoan { get; set; }

        [Column("BanRongBang")]
        [Required]
        public double BanRongBang { get; set; }

        [Column("ChieuXoan")]
        [Required]
        public bool ChieuXoan { get; set; }

        [Column("DoDayBang")]
        [Required]
        public double DoDayBang { get; set; }

        [Column("GoiCachMep")]
        [Required]
        public double GoiCachMep { get; set; }

        [Column("DKBTP")]
        [Required]
        public double DKBTP { get; set; }

        [Column("ThongTinSPTrongCaLamViec_ID")]
        [Required]
        [ForeignKey(nameof(ThongTinSPTrongCaLamViec))]
        public int ThongTinSPTrongCaLamViecID { get; set; }

        public virtual ThongTinSPTrongCaLamViec_Model? ThongTinSPTrongCaLamViec { get; set; }

    }
}
