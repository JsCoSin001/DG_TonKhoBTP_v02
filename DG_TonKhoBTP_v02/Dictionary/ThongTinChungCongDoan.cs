using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG_TonKhoBTP_v02.Dictionary
{
    public static class
    ThongTinChungCongDoan
    {
        // Helper: danh sách máy
        private static List<string> May(params string[] codes)
            => new List<string>(codes);

        // Helper: nhóm cột chung cho hầu hết công đoạn
        private static List<ColumnDefinition> BaseColumns(params ColumnDefinition[] extras)
        {
            var list = new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau",    DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau",    DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id",          DataType = typeof(int),    Header = "ID" },
                new ColumnDefinition { Name = "DonVi",       DataType = typeof(string), Header = "DonVi" },
                new ColumnDefinition { Name = "Ngay", DataType = typeof(string), Header = "Ngay" },
                new ColumnDefinition { Name = "TenNVL", DataType = typeof(string), Header = "TenNVL" },
                new ColumnDefinition { Name = "Ca", DataType = typeof(string), Header = "Ca" },
                new ColumnDefinition { Name = "NguoiLam", DataType = typeof(string), Header = "NguoiLam" },
                new ColumnDefinition { Name = "GhiChu", DataType = typeof(string), Header = "GhiChu" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL",      DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai",    DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai",    DataType = typeof(double), Header = "KL còn lại" },
            };

            list.AddRange(extras);
            return list;
        }

        // Helper: các cột SELECT chung kiểu TTThanhPham/TTNVL/DSNVL
        private static List<string> Select_TP_CoKhoiLuong()
            => new List<string>
            {
            "TTThanhPham.MaBin",
            "TTThanhPham.ChieuDaiTruoc",
            "TTThanhPham.ChieuDaiSau",
            "TTThanhPham.KhoiLuongTruoc",
            "TTThanhPham.KhoiLuongSau",
            "TTThanhPham.Phe",
            "TTNVL.BinNVL",
            "DSNVL.Ten  as Ten_NVL"
            };

        private static List<string> Select_TP_CoCaiDatCDBoc()
            => new List<string>
            {
            "TTThanhPham.MaBin",
            "TTThanhPham.ChieuDaiTruoc",
            "TTThanhPham.ChieuDaiSau",
            "CaiDatCDBoc.NhuaPhe",
            "CaiDatCDBoc.DayPhe",
            "CaiDatCDBoc.GhiChuNhuaPhe",
            "CaiDatCDBoc.GhiChuDayPhe",
            "TTThanhPham.Phe",
            "TTNVL.BinNVL",
            "DSNVL.Ten  as Ten_NVL"
            };

        private static List<string> Select_TP_KhongKhoiLuong()
            => new List<string>
            {
            "TTThanhPham.MaBin",
            "TTThanhPham.ChieuDaiTruoc",
            "TTThanhPham.ChieuDaiSau",
            "TTThanhPham.Phe",
            "TTNVL.BinNVL",
            "DSNVL.Ten as Ten_NVL"
            };

        // ================= CÁC CÔNG ĐOẠN =================

        public static readonly CongDoan KeoRut = new CongDoan(
            0,
            "Kéo rút",
            May("MAY01", "MAY02", "MAY03", "MAY04", "MAY05"),
            BaseColumns(),                     // chỉ dùng cột base
            new List<string> { "BTP.20101%", "BTP.20201%" },
            Select_TP_CoKhoiLuong()
        );

        public static readonly CongDoan BenRuot = new CongDoan(
            1,
            "bện đồng - nhôm",
            May("Ben_1", "Ben_2", "Ben_3", "Ben_4", "Ben_5"),
            BaseColumns(
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" }
            ),
            new List<string> { "BTP.20102%", "BTP.20202%" },
            Select_TP_CoKhoiLuong()
        );

        public static readonly CongDoan BocMach = new CongDoan(
            3,
            "Bọc cách điện",
            May("BM1", "BM5", "BM4", "BM3", "BM2"),
            BaseColumns(
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "SoSoi", DataType = typeof(double), Header = "Số sợi" }
            ),
            new List<string> { "BTP.20103%", "BTP.20203%" },
            Select_TP_CoCaiDatCDBoc()
        );

        public static readonly CongDoan BocLot = new CongDoan(
            4,
            "Bọc lót",
            May("BL1", "BL2"),
            BaseColumns(),                         // giống KeoRut: chỉ base
            new List<string> { "BTP.20105%", "BTP.20205%" },
            Select_TP_CoCaiDatCDBoc()
        );

        public static readonly CongDoan BocVo = new CongDoan(
            5,
            "Bọc vỏ",
            May("BV1", "BV2"),
            BaseColumns(
                new ColumnDefinition { Name = "KetCauLoi", DataType = typeof(double), Header = "Kết cấu lõi" }
            ),
            new List<string> { "TP.%" },
            Select_TP_CoCaiDatCDBoc()
        );

        public static readonly CongDoan GhepLoi_QB = new CongDoan(
            2,
            "ghép lõi - quấn băng",
            May("QB1", "QB2", "QB3", "QB4", "QB5"),
            BaseColumns(
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "DuongKinhSoiMach", DataType = typeof(double), Header = "ĐK sợi mạch" },
                new ColumnDefinition { Name = "BanRongBang", DataType = typeof(double), Header = "Độ rộng băng" },
                new ColumnDefinition { Name = "DoDayBang", DataType = typeof(double), Header = "Độ dày băng" }
            ),
            new List<string> { "BTP.20107%", "BTP.20207%" },
            Select_TP_KhongKhoiLuong()
        );

        public static readonly CongDoan GhepLoi = new CongDoan(GhepLoi_QB)
        {
            Id = 6,
            TenCongDoan = "ghép lõi",
            ListMa_Accept = new List<string> { "BTP.20104%", "BTP.20204%" }
        };

        public static readonly CongDoan QuanBang = new CongDoan(GhepLoi_QB)
        {
            Id = 7,
            TenCongDoan = "Quấn băng thép - đồng - nhôm",
            ListMa_Accept = new List<string> { "BTP.20106%", "BTP.20206%" }
        };

        public static readonly CongDoan Mica = new CongDoan(GhepLoi_QB)
        {
            Id = 8,
            TenCongDoan = "quấn băng mica",
            // nếu cần ListMa_Accept riêng thì set thêm ở đây
        };

        public static readonly List<CongDoan> TatCaCongDoan = new List<CongDoan>
        {
            KeoRut,
            BenRuot,
            GhepLoi_QB,
            BocMach,
            BocLot,
            BocVo,
            GhepLoi,
            QuanBang,
            Mica
        };

        public static string GetTenCongDoanById(int id)
        {
            var cd = TatCaCongDoan.FirstOrDefault(x => x.Id == id);
            return cd?.TenCongDoan ?? "Không tìm thấy công đoạn";
        }



    }


}
