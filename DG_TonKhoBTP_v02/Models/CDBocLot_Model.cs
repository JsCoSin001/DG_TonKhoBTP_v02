using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDBocLot_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("DoDayTBLot")]
        [Required]
        public double DoDayTBLot { get; set; }

        [Column("CaiDatCDBoc_ID")]
        [ForeignKey(nameof(CaiDatCDBoc))]
        public int? CaiDatCDBocID { get; set; }

        public virtual CaiDatCDBoc_Model? CaiDatCDBoc { get; set; }

    }
}
