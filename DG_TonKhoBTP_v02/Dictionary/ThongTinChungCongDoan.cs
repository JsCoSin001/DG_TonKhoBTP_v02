using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Dictionary
{
    public static class ThongTinChungCongDoan
    {
        public static readonly CongDoan KeoRut = new CongDoan(
            "Kéo rút",
            new List<string> { "MAY01", "MAY02", "MAY03", "MAY04", "MAY05" }
        );

        public static readonly CongDoan BenRuot = new CongDoan(
            "bện ruột dẫn",
            new List<string> { "Ben_1", "Ben_2", "Ben_3", "Ben_4", "Ben_5" }
        );

        public static readonly CongDoan GhepLoi_QB = new CongDoan(
            "ghép lõi - quấn băng",
            new List<string> { "QB1", "QB2", "QB3", "QB4", "QB5" }
        );

        public static readonly CongDoan BocMach = new CongDoan(
            "Bọc mạch",
            new List<string> { "BM1", "BM5", "BM4", "BM3", "BM2" }
        );

        public static readonly CongDoan BocLot = new CongDoan(
            "Bọc lot",
            new List<string> { "BL1", "BL2" }
        );

        public static readonly CongDoan BocVo = new CongDoan(
            "Bọc vỏ",
            new List<string> { "BV1", "BV2" }
        );

    }

    public class CongDoan
    {
        public string TenCongDoan { get; set; }
        public List<string> DanhSachMay { get; set; }
        public CongDoan(string tenCongDoan, List<string> danhSachMay)
        {
            TenCongDoan = ("BÁO CÁO CÔNG ĐOẠN " + tenCongDoan).ToUpper();
            DanhSachMay = danhSachMay;
        }
    }
}
