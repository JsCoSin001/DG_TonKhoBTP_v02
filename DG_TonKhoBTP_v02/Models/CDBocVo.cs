using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("CD_BocVo")]
    public class CDBocVo
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        public int? ThongTinSPID { get; set; }

    [Column("DayVoTB")]
        [Required]
        public double DayVoTB { get; set; }

    [Column("InAn")]
        [Required]
        public bool InAn { get; set; }
    }
}
