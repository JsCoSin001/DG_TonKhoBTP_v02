using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_BenRuot")]
    public class CDBenRuot
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        [Required]
        public int ThongTinSPID { get; set; }

    [Column("DKSoi")]
        [Required]
        public double DKSoi { get; set; }

    [Column("SoSoi")]
        public decimal? SoSoi { get; set; }

    [Column("Chiều Xoắn")]
        public string ChiUXoN { get; set; }

    [Column("BuocBen")]
        [Required]
        public double BuocBen { get; set; }
    }
}
