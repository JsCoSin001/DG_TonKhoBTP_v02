using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("ThongTinCaLamViec")]
    public class ThongTinCaLamViec
{
    [Column("id")]
        public int Id { get; set; }

    [Column("Ngay")]
        [DataType(DataType.DateTime)]
        public DateTime Ngay { get; set; }

    [Column("May")]
        public string May { get; set; }

    [Column("Ca")]
        public string Ca { get; set; }

    [Column("NguoiLam")]
        public string NguoiLam { get; set; }

    [Column("ToTruong")]
        public string ToTruong { get; set; }

    [Column("QuanDoc")]
        public string QuanDoc { get; set; }
    }
}
