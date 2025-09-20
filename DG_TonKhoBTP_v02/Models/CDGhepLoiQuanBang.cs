using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_GhepLoi_QuanBang")]
    public class CDGhepLoiQuanBang
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        [Required]
        public int ThongTinSPID { get; set; }

    [Column("BuocXoan")]
        [Required]
        public double BuocXoan { get; set; }

    [Column("ChieuXoan")]
        public string ChieuXoan { get; set; }

    [Column("GoiCachMep")]
        [Required]
        public double GoiCachMep { get; set; }

    [Column("DKBTP")]
        [Required]
        public double DKBTP { get; set; }
    }
}
