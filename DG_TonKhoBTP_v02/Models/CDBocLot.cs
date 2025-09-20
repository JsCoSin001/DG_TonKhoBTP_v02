using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_BocLot")]
    public class CDBocLot
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        public int? ThongTinSPID { get; set; }

    [Column("DoDayTBLot")]
        [Required]
        public double DoDayTBLot { get; set; }
    }
}
