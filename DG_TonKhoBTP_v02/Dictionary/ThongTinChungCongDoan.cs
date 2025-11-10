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
        public static readonly CongDoan KeoRut = new CongDoan(
            0,
            "Kéo rút",
            new List<string> { "MAY01", "MAY02", "MAY03", "MAY04", "MAY05" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu"},
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
            },
            new List<string> { "BTP.20101%", "BTP.20201%" },
            new List<string> { 
                //"DanhSachMaSP.Ma", 
                //"DanhSachMaSP.Ten", 
                "TTThanhPham.MaBin",
                "TTThanhPham.ChieuDaiTruoc",
                "TTThanhPham.ChieuDaiSau",
                "TTThanhPham.KhoiLuongTruoc",
                "TTThanhPham.KhoiLuongSau",
                "TTThanhPham.Phe",
                "TTNVL.BinNVL",
                "DSNVL.Ten  as Ten_NVL"
            }

        );

        public static readonly CongDoan BenRuot = new CongDoan(
            1,
            "bện đồng - nhôm",
            new List<string> { "Ben_1", "Ben_2", "Ben_3", "Ben_4", "Ben_5" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
            },
            new List<string> { "BTP.20102%", "BTP.20202%" },
            new List<string> { 
                //"DanhSachMaSP.Ma", 
                //"DanhSachMaSP.Ten", 
                "TTThanhPham.MaBin",
                "TTThanhPham.ChieuDaiTruoc",
                "TTThanhPham.ChieuDaiSau",
                "TTThanhPham.KhoiLuongTruoc",
                "TTThanhPham.KhoiLuongSau",
                "TTThanhPham.Phe",
                "TTNVL.BinNVL",
                "DSNVL.Ten  as Ten_NVL"
            }

        );

        public static readonly CongDoan BocMach = new CongDoan(
            3,
            "Bọc cách điện",
            new List<string> { "BM1", "BM5", "BM4", "BM3", "BM2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "SoSoi", DataType = typeof(double), Header = "Số sợi" },
            },
            new List<string> { "BTP.20103%", "BTP.20203%" },
            new List<string> {
                //"DanhSachMaSP.Ma",
                //"DanhSachMaSP.Ten",
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
            }

        );

        public static readonly CongDoan BocLot = new CongDoan(
            4,
            "Bọc lót",
            new List<string> { "BL1", "BL2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
            },
            new List<string> { "BTP.20105%", "BTP.20205%" },
            new List<string> {
                //"DanhSachMaSP.Ma",
                //"DanhSachMaSP.Ten",
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
            }

        );

        public static readonly CongDoan BocVo = new CongDoan(
            5,
            "Bọc vỏ",
            new List<string> { "BV1", "BV2" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
                new ColumnDefinition { Name = "KetCauLoi", DataType = typeof(double), Header = "Kết cấu lõi" },
            },
            new List<string> { "TP.%" },
            new List<string> {
                //"DanhSachMaSP.Ma",
                //"DanhSachMaSP.Ten",
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
            }

        );

        public static readonly CongDoan GhepLoi_QB = new CongDoan(
            2,
            "ghép lõi - quấn băng",
            new List<string> { "QB1", "QB2", "QB3", "QB4", "QB5" },
            new List<ColumnDefinition>
            {
                new ColumnDefinition { Name = "KlBatDau", DataType = typeof(double), Header = "KL bắt đầu" },
                new ColumnDefinition { Name = "CdBatDau", DataType = typeof(double), Header = "CD bắt đầu" },
                new ColumnDefinition { Name = "id", DataType = typeof(int), Header = "ID" },
                new ColumnDefinition { Name = "DanhSachMaSP_ID", DataType = typeof(int), Header = "DanhSachMaSP_ID" },
                new ColumnDefinition { Name = "BinNVL", DataType = typeof(string), Header = "LOT nguyên liệu" },
                new ColumnDefinition { Name = "CdConLai", DataType = typeof(double), Header = "CD còn lại" },
                new ColumnDefinition { Name = "KlConLai", DataType = typeof(double), Header = "KL còn lại" },
                new ColumnDefinition { Name = "DuongKinhSoiDong", DataType = typeof(double), Header = "ĐK sợi đồng" },
                new ColumnDefinition { Name = "DuongKinhSoiMach", DataType = typeof(double), Header = "ĐK sợi mạch" },
                new ColumnDefinition { Name = "BanRongBang", DataType = typeof(double), Header = "Độ rộng băng" },
                new ColumnDefinition { Name = "DoDayBang", DataType = typeof(double), Header = "Độ dày băng" },
            },
            new List<string> { "BTP.20107%", "BTP.20207%" },
            new List<string> {
                //"DanhSachMaSP.Ma",
                //"DanhSachMaSP.Ten",
                "TTThanhPham.MaBin",
                "TTThanhPham.ChieuDaiTruoc",
                "TTThanhPham.ChieuDaiSau",
                "TTThanhPham.Phe",
                "TTNVL.BinNVL",
                "DSNVL.Ten as Ten_NVL"
            }

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
        };

    }


}
