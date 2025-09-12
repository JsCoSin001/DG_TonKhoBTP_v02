using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class ThongTinCaLamViec_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("Ngay")]
        [Required]
        public DateTime Ngay { get; set; }

        [Column("May")]
        public string May { get; set; }

        [Column("Ca")]
        public string Ca { get; set; }

        [Column("NguoiLam")]
        public string NguoiLam { get; set; }

        [Column("ToTruong")]
        public string? ToTruong { get; set; }

        [Column("QuanDoc")]
        public string? QuanDoc { get; set; }

        public virtual ICollection<ThongTinSPTrongCaLamViec_Model> ThongTinSPTrongCaLamViecs { get; set; } = new List<ThongTinSPTrongCaLamViec_Model>();

    }
}
