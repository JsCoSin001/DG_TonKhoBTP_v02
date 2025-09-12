using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDBocMach_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("KiemTraPhoihoi")]
        public string KiemTraPhoihoi { get; set; }

        [Column("NgoaiQuan")]
        [Required]
        public bool NgoaiQuan { get; set; }

        [Column("DanhThung")]
        public string DanhThung { get; set; }

        [Column("CaiDatCDBoc_ID")]
        [Required]
        [ForeignKey(nameof(CaiDatCDBoc))]
        public int CaiDatCDBocID { get; set; }

        public virtual CaiDatCDBoc_Model? CaiDatCDBoc { get; set; }

    }
}
