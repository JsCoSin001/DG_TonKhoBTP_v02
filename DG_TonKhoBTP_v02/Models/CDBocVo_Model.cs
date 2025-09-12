using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDBocVo_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("KetCauLoi")]
        public string KetCauLoi { get; set; }

        [Column("DayVoTB")]
        [Required]
        public double DayVoTB { get; set; }

        [Column("InAn")]
        [Required]
        public bool InAn { get; set; }

        [Column("CaiDatCDBoc_ID")]
        [ForeignKey(nameof(CaiDatCDBoc))]
        public int? CaiDatCDBocID { get; set; }

        public virtual CaiDatCDBoc_Model? CaiDatCDBoc { get; set; }

    }
}
