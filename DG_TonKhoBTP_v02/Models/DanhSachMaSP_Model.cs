using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeneratedModels
{
public class DanhSachMaSP_Model
    {
        [Column("id")]
        [Key]
        [Required]
        public int Id { get; set; }

        [Column("Ten")]
        public string Ten { get; set; }

        [Column("Ma")]
        public string Ma { get; set; }

        [Column("KieSP")]
        public string KieSP { get; set; }

        [Column("DateInsert")]
        [Required]
        public DateTime DateInsert { get; set; } = DateTime.Now;

        public virtual ICollection<ThongTinSPTrongCaLamViec_Model> ThongTinSPTrongCaLamViecs { get; set; } = new List<ThongTinSPTrongCaLamViec_Model>();

    }
}
