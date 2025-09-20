using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("DanhSachMaSP")]
    public class DanhSachMaSP
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("Ten")]
        public string Ten { get; set; }

    [Column("Ma")]
        public string Ma { get; set; }

    [Column("DonVi")]
        [Required]
        public bool DonVi { get; set; }

    [Column("KieuSP")]
        public string KieuSP { get; set; }

    [Column("DateInsert")]
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DateInsert { get; set; }
    }
}
