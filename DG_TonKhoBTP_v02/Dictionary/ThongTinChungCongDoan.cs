using DG_TonKhoBTP_v02.Models;
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
            new List<string> { "MAY01", "MAY02", "MAY03", "MAY04", "MAY05" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" }
            }
        );

        public static readonly CongDoan BenRuot = new CongDoan(
            "bện ruột dẫn",
            new List<string> { "Ben_1", "Ben_2", "Ben_3", "Ben_4", "Ben_5" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" }
            }
        );

        public static readonly CongDoan GhepLoi_QB = new CongDoan(
            "ghép lõi - quấn băng",
            new List<string> { "QB1", "QB2", "QB3", "QB4", "QB5" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "DuongKinhSoiMach", DataType = typeof(double), Header = "ĐK sợi mạch" },
                new ColumnDefinition { Name = "BanRongBang", DataType = typeof(double), Header = "Bản rộng băng" },
                new ColumnDefinition { Name = "DoDayBang", DataType = typeof(double), Header = "Độ dày băng" },
            }
        );

        public static readonly CongDoan BocMach = new CongDoan(
            "Bọc mạch",
            new List<string> { "BM1", "BM5", "BM4", "BM3", "BM2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "SoSoi", DataType = typeof(double), Header = "Số sợi" }
            }
        );

        public static readonly CongDoan BocLot = new CongDoan(
            "Bọc lot",
            new List<string> { "BL1", "BL2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" }
            }
        );

        public static readonly CongDoan BocVo = new CongDoan(
            "Bọc vỏ",
            new List<string> { "BV1", "BV2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "binNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "KetCauLoi", DataType = typeof(double), Header = "Kết cấu lõi" }
            }
        );

    }

    
}
