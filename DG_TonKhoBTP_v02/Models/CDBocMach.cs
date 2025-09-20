using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_BocMach")]
    public class CDBocMach
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        [Required]
        public int ThongTinSPID { get; set; }

    [Column("NgoaiQuan")]
        [Required]
        public bool NgoaiQuan { get; set; }

    [Column("LanDanhThung")]
        [Required]
        public int LanDanhThung { get; set; }

    [Column("SoMet")]
        [Required]
        public double SoMet { get; set; }
    }
}
