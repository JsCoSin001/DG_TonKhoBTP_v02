using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_KeoRut")]
    public class CDKeoRut
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        [Required]
        public int ThongTinSPID { get; set; }

    [Column("DKTrucX")]
        [Required]
        public double DKTrucX { get; set; }

    [Column("DKTrucY")]
        [Required]
        public double DKTrucY { get; set; }

    [Column("NgoaiQuan")]
        [Required]
        public bool NgoaiQuan { get; set; }

    [Column("TocDo")]
        [Required]
        public double TocDo { get; set; }

    [Column("DienApU")]
        [Required]
        public double DienApU { get; set; }

    [Column("DongDienU")]
        [Required]
        public double DongDienU { get; set; }
    }
}
