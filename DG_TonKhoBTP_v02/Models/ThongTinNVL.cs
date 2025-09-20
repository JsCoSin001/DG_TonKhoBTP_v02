using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Models
{
    [Table("ThongTinNVL")]
    public class ThongTinNVL
{
    [Column("id")]
        [Required]
        public int Id { get; set; }

    [Column("ThongTinSP_ID")]
        [Required]
        public int ThongTinSPID { get; set; }

    [Column("BinNVL")]
        public string BinNVL { get; set; }

    [Column("DuongKinhSoiDong")]
        public double? DuongKinhSoiDong { get; set; }

    [Column("SoSoi")]
        public int? SoSoi { get; set; }

    [Column("KetCauLoi")]
        public double? KetCauLoi { get; set; }

    [Column("DuongKinhSoiMach")]
        public double? DuongKinhSoiMach { get; set; }

    [Column("BanRongBang")]
        public double? BanRongBang { get; set; }

    [Column("DoDayBang")]
        public double? DoDayBang { get; set; }
    }
}
