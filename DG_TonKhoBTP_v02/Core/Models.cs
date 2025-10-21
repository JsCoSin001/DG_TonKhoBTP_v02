// File: Core/Models.cs
// Mục đích: Các DTO tối thiểu để đóng gói snapshot theo cấu trúc DB đã có.
// Tham chiếu cột theo file SQL (CaiDatCDBoc, TTNVL, ThongTinCaLamViec, ...).
// [Luồng 3] Snapshot gom dữ liệu từ nhiều UC vào một biến.

using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Core
{
    // Khớp bảng TTNVL (cột nào không nhập thì lưu 0)
    public class TTNVL
    {
        public int Id { get; set; }                 // Optional (nếu có)
        public int TTThanhPhan_ID { get; set; }      // Optional (chưa gán lúc nhập tay)
        public string BinNVL { get; set; }          // NOT NULL trong DB
        public double KlBatDau { get; set; }
        public double CdBatDau { get; set; }
        public double KlConLai { get; set; }
        public double CdConLai { get; set; }
        public double DuongKinhSoiDong { get; set; }
        public int SoSoi { get; set; }
        public double KetCauLoi { get; set; }
        public double DuongKinhSoiMach { get; set; }
        public double BanRongBang { get; set; }
        public double DoDayBang { get; set; }
    }

    // Khớp bảng CaiDatCDBoc (rút gọn thuộc tính thường dùng từ UC_CaiDatMay/UC_DieuKienBoc/UC_CDBocLot)
    public class CaiDatCDBoc
    {
        public bool MangNuoc { get; set; }
        public bool PuliDanDay { get; set; }
        public bool BoDemMet { get; set; }
        public bool? MayIn { get; set; }
        public double? v1 { get; set; }
        public double? v2 { get; set; }
        public double? v3 { get; set; }
        public double? v4 { get; set; }
        public double? v5 { get; set; }
        public double? v6 { get; set; }
        public double? Co { get; set; }
        public double? Dau1 { get; set; }
        public double? Dau2 { get; set; }
        public double? Khuon { get; set; }
        public double? BinhSay { get; set; }
        public double DKKhuon1 { get; set; }
        public double DKKhuon2 { get; set; }
        public string TTNhua { get; set; }
        public double NhuaPhe { get; set; }
        public string GhiChuNhuaPhe { get; set; }
        public double DayPhe { get; set; }
        public string GhiChuDayPhe { get; set; }
        public double KTDKLan1 { get; set; }
        public double KTDKLan2 { get; set; }
        public double KTDKLan3 { get; set; }
        public double DiemMongLan1 { get; set; }
        public double DiemMongLan2 { get; set; }
    }

    // Ca làm việc (tối thiểu cho UC_TTCaLamViec)
    public class ThongTinCaLamViec
    {
        public string Ngay { get; set; }
        public string May { get; set; }
        public string Ca { get; set; }
        public string NguoiLam { get; set; }
        public string ToTruong { get; set; }
        public string QuanDoc { get; set; }
    }


    public class TTThanhPham
    {
        public int Id { get; set; }
        public int DanhSachSP_ID { get; set; }        // FK -> DanhSachMaSP.id
        public int ThongTinCaLamViec_ID { get; set; } // FK -> ThongTinCaLamViec.id
        public string MaBin { get; set; }             // NOT NULL
        public double KhoiLuongTruoc { get; set; }    // NOT NULL
        public double KhoiLuongSau { get; set; }      // NOT NULL DEFAULT 0
        public double ChieuDaiTruoc { get; set; }     // NOT NULL
        public double ChieuDaiSau { get; set; }       // NOT NULL
        public double Phe { get; set; }               // NOT NULL DEFAULT 0
        public CongDoan CongDoan { get; set; }               // NOT NULL DEFAULT 0
        public string GhiChu { get; set; }            // NULL
        public string? DateInsert { get; set; }     // NULL
    }

    // --------------------------- Công đoạn: Bóc Vỏ ---------------------------

    public class CD_BocVo
    {
        public int Id { get; set; }
        public int? TTThanhPhan_ID { get; set; }   // FK -> ThongTinSP.id (NULLABLE trong schema)
        public double DayVoTB { get; set; }       // NOT NULL
        public string InAn { get; set; }            // NOT NULL DEFAULT 1
    }

    // --------------------------- Công đoạn: Bóc Lót ---------------------------

    public class CD_BocLot
    {
        public int Id { get; set; }
        public int? TTThanhPhan_ID { get; set; }   // FK -> ThongTinSP.id (NULLABLE trong schema)
        public double DoDayTBLot { get; set; }    // NOT NULL
    }

    // --------------------------- Công đoạn: Bóc Mạch ---------------------------

    public class CD_BocMach
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        // NgoaiQuan: 1 - OK ; 0 - NG
        public string NgoaiQuan { get; set; }       // NOT NULL DEFAULT 1
        // (ghi số lần và số mét máy báo)
        public int LanDanhThung { get; set; }     // NOT NULL
        public double SoMet { get; set; }         // NOT NULL
    }

    // --------------------------- Công đoạn: Kéo Rút ---------------------------

    public class CD_KeoRut
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        public double DKTrucX { get; set; }       // NOT NULL
        public double DKTrucY { get; set; }       // NOT NULL
        public string NgoaiQuan { get; set; }       // NOT NULL DEFAULT 1
        public double TocDo { get; set; }         // NOT NULL
        public double DienApU { get; set; }       // NOT NULL
        public double DongDienU { get; set; }     // NOT NULL
    }

    // --------------------------- Công đoạn: Bện Ruột ---------------------------

    public class CD_BenRuot
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        public double DKSoi { get; set; }         // NOT NULL
        public int? SoSoi { get; set; }           // NUMERIC -> int?
        public string ChieuXoan { get; set; }     // NOT NULL 
        public double BuocBen { get; set; }       // NOT NULL
    }

    // --------------------------- Công đoạn: Ghép Lõi + Quấn Băng ---------------------------

    public class CD_GhepLoiQB
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        public double BuocXoan { get; set; }      // NOT NULL
        public string ChieuXoan { get; set; }     // NOT NULL DEFAULT 'Z'
        public double GoiCachMep { get; set; }    // NOT NULL
        public double DKBTP { get; set; }         // NOT NULL
    }

    public class FormSnapshot
    {
        // Lưu từng section theo tên -> object (có thể là TTNVL list, CaiDatCDBoc, v.v.)
        public Dictionary<string, object> Sections { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public T GetSection<T>(string name) where T : class
        {
            return Sections.TryGetValue(name, out var o) ? o as T : null;
        }
    }
}
