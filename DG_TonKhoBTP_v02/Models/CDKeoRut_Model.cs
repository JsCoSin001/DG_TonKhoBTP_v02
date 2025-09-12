using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class CDKeoRut_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

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

        [Column("TrongLuong")]
        [Required]
        public double TrongLuong { get; set; }

        [Column("ThongTinSPTrongCaLamViec_ID")]
        [Required]
        [ForeignKey(nameof(ThongTinSPTrongCaLamViec))]
        public int ThongTinSPTrongCaLamViecID { get; set; }

        public virtual ThongTinSPTrongCaLamViec_Model? ThongTinSPTrongCaLamViec { get; set; }

    }
}
