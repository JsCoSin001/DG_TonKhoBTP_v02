#nullable enable

// File: Core/Models.cs
// Mục đích: Các DTO tối thiểu để đóng gói snapshot theo cấu trúc DB đã có.
// Tham chiếu cột theo file SQL (CaiDatCDBoc, TTNVL, ThongTinCaLamViec, ...).
// [Luồng 3] Snapshot gom dữ liệu từ nhiều UC vào một biến.

using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DG_TonKhoBTP_v02.Core
{

    public class TTNVL
    {
        // Để nullable (int?) để có thể gán null nếu người dùng không nhập
        public int? Id { get; set; }                     
        public int? TTThanhPhan_ID { get; set; }         
        public int? DanhSachMaSP_ID { get; set; }

        public string BinNVL { get; set; }       
        public int? CongDoan { get; set; } = -1;
        public double? KlBatDau { get; set; } = -1;
        public string Ngay { get; set; } = "";
        public string Ca { get; set; } = "";
        public string NguoiLam { get; set; } = "";
        public string TenNVL { get; set; } = "";
        public string? GhiChu { get; set; } = "";
        public double? CdBatDau { get; set; } = -1;
        public double? KlConLai { get; set; } = -1;
        public double? CdConLai { get; set; } = -1;
        public double? DuongKinhSoiDong { get; set; } = -1;
        public int? SoSoi { get; set; } = -1;
        public double? KetCauLoi { get; set; } = -1;
        public double? DuongKinhSoiMach { get; set; } = -1;
        public double? BanRongBang { get; set; } = -1;
        public double? DoDayBang { get; set; } = -1;
        public string? DonVi { get; set; } = "";
        // Lưu thay cho TTThanhPham.QC
        public string? QC { get; set; }
    }

    public class Submit {
        public bool IsChecked { get; set; }
    }

    public class Backup
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("DanhSachSP_ID")]
        public int DanhSachSpId { get; set; }

        [Required]
        [Column("TTThanhPham_ID")]
        public int TtThanhPhamId { get; set; }

        [Column("ChieuDai")]
        public double? ChieuDai { get; set; }   // REAL -> double?

        [Column("KhoiLuong")]
        public double? KhoiLuong { get; set; }  // REAL -> double?

        [Required]
        [Column("DateInsert")]
        public string DateInsert { get; set; } = default!; // TEXT NOT NULL (có thể đổi sang DateTime nếu bạn lưu ISO)

        [Required]
        [Column("LastEdit_ID")]
        public int LastEditId { get; set; }

        // Navigation properties (nếu bạn có entity tương ứng)
        public virtual TTThanhPham? TTThanhPham { get; set; }
        public virtual DanhSachMaSP? DanhSachMaSP { get; set; }
    }

    public class CaiDatCDBoc
    {
        public string? MangNuoc { get; set; }
        public string? PuliDanDay { get; set; }
        public string? BoDemMet { get; set; }
        public string? MayIn { get; set; }
        public double? v1 { get; set; }
        public double? v2 { get; set; }
        public double? v3 { get; set; }
        public double? v4 { get; set; }
        public double? v5 { get; set; }
        public double v6 { get; set; } = -1;
        public double? Co { get; set; }
        public double? Dau1 { get; set; }
        public double Dau2 { get; set; } = -1;
        public double? Khuon { get; set; }
        public double? BinhSay { get; set; }
        public double? DKKhuon1 { get; set; }
        public double? DKKhuon2 { get; set; }
        public string? TTNhua { get; set; }
        public double? NhuaPhe { get; set; }
        public string GhiChuNhuaPhe { get; set; } = "";
        public double? DayPhe { get; set; }
        public string GhiChuDayPhe { get; set; } = "";
        public double? KTDKLan1 { get; set; }
        public double? KTDKLan2 { get; set; }
        public double? KTDKLan3 { get; set; }
        public double? DiemMongLan1 { get; set; }
        public double? DiemMongLan2 { get; set; }
    }

    // Ca làm việc (tối thiểu cho UC_TTCaLamViec)
    public class ThongTinCaLamViec
    {
        public int Id { get; set; }
        public string Ngay { get; set; }
        public int TTThanhPham_id { get; set; }
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
        public string TenTP { get; set; }       
        public string MaTP { get; set; }       
        public string DonVi { get; set; }       
        public string MaBin { get; set; }             // NOT NULL
        public double KhoiLuongTruoc { get; set; }    // NOT NULL
        public double KhoiLuongSau { get; set; }      // NOT NULL DEFAULT 0
        public double ChieuDaiTruoc { get; set; }     // NOT NULL
        public double ChieuDaiSau { get; set; }       // NOT NULL
        public double Phe { get; set; }               // NOT NULL DEFAULT 0
        public CongDoan CongDoan { get; set; }               // NOT NULL DEFAULT 0
        public string GhiChu { get; set; } = "";            // NULL
        public string QC { get; set; } = "";    // NULL
        public int Active { get; set; } = 1;
        public double HanNoi { get; set; } = 0;
        public int? LastEdit_id { get; set; }
        public string? DateInsert { get; set; }     // NULL
    }

    // --------------------------- Công đoạn: Bóc Vỏ ---------------------------
    public class CD_BocVo
    {
        public int Id { get; set; }
        public int? TTThanhPhan_ID { get; set; }   // FK -> ThongTinSP.id (NULLABLE trong schema)
        public double? DayVoTB { get; set; }       // NOT NULL
        public string? InAn { get; set; }            // NOT NULL DEFAULT 1
    }

    // --------------------------- Công đoạn: Bóc Lót ---------------------------
    public class CD_BocLot
    {
        public int Id { get; set; }
        public int? TTThanhPhan_ID { get; set; }   // FK -> ThongTinSP.id (NULLABLE trong schema)
        public double? DoDayTBLot { get; set; }    // NOT NULL
    }

    // --------------------------- Công đoạn: Bóc Mạch ---------------------------
    public class CD_BocMach
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        // NgoaiQuan: 1 - OK ; 0 - NG
        public string? NgoaiQuan { get; set; }       // NOT NULL DEFAULT 1
        
        public int? LanDanhThung { get; set; }     // NOT NULL
        public double? SoMet { get; set; }         // NOT NULL
    }

    // --------------------------- Công đoạn: Kéo Rút ---------------------------
    public class CD_KeoRut
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    
        public double? DKTrucX { get; set; }       
        public double? DKTrucY { get; set; }       
        public string? NgoaiQuan { get; set; }       
        public double? TocDo { get; set; }         
        public double DienApU { get; set; } = -1;       
        public double DongDienU { get; set; } = -1;   
    }

    // --------------------------- Công đoạn: Bện Ruột ---------------------------

    public class CD_BenRuot
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        public double? DKSoi { get; set; }         // NOT NULL
        public int? SoSoi { get; set; }           // NUMERIC -> int?
        public string? ChieuXoan { get; set; }     // NOT NULL 
        public double?  BuocBen { get; set; }       // NOT NULL
    }

    // --------------------------- Công đoạn: Ghép Lõi + Quấn Băng ---------------------------
    public class CD_GhepLoiQB
    {
        public int Id { get; set; }
        public int TTThanhPhan_ID { get; set; }    // NOT NULL
        public double? BuocXoan { get; set; }      // NOT NULL
        public string? ChieuXoan { get; set; }     // NOT NULL DEFAULT 'Z'
        public double? GoiCachMep { get; set; }    // NOT NULL
        public double? DKBTP { get; set; }         // NOT NULL
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

    public class DanhSachMaSP
    {
        public int Id { get; set; }
        public string Ten { get; set; }
        public string Ma { get; set; }
        public string DonVi { get; set; }
        public string KieuSP { get; set; }
        public DateTime? DateInsert { get; set; }
    }

    public class EditModel
    {
        public int Id { get; set; }
    }

    // 1) Phần chung dùng lại cho cả Search/Result/Entity
    public abstract class KeHoachSXBase
    {
        public string? Ten { get; set; }
        public string? NgayNhan { get; set; }
        public string? Lot { get; set; }
        public double? SLHangDat { get; set; }
        public double? SLHangBan { get; set; }
        public double? SLTong { get; set; }      // chỉ class nào cần thì dùng
        public string? Mau { get; set; }
        public string? NgayGiao { get; set; }
        public string? TenKhachHang { get; set; }
        public string? GhiChu { get; set; }
    }

    // 2) Entity chính
    public class KeHoachSX : KeHoachSXBase
    {
        public int Id { get; set; }
        public int DanhSachMaSP_ID { get; set; }

        // mapping/lookup
        public string Ma { get; set; } = string.Empty;

        // riêng
        public string? GhiChu_QuanDoc { get; set; }

        // trạng thái (entity là bắt buộc => int)
        public int TinhTrang { get; set; }
        public int MucDoUuTienKH { get; set; }
        public int TrangThaiSX { get; set; }

        // Nếu bạn muốn giữ Lot/NgayGiao non-null như code cũ:
        // public new string Lot { get => base.Lot ?? ""; set => base.Lot = value; }
        // public new string NgayGiao { get => base.NgayGiao ?? ""; set => base.NgayGiao = value; }
    }

    // 3) DTO tìm kiếm (filter)
    public class TimKiemKeHoachSX : KeHoachSXBase
    {
        // filter trạng thái là nullable => int?
        public int? TinhTrang { get; set; }
        public int? MucDoUuTienKH_Filter { get; set; }
        public int? TrangThaiSX { get; set; }

        // Alias để giữ tên property cũ (đỡ sửa code đang dùng)
        public int? TinhTrangCuaKH
        {
            get => TinhTrang;
            set => TinhTrang = value;
        }

        public int? TrangThaiThucHienKH
        {
            get => TrangThaiSX;
            set => TrangThaiSX = value;
        }

        // Nếu muốn giữ đúng tên cũ MucDoUuTienKH (nhưng tránh trùng với entity):
        public int? MucDoUuTienKH
        {
            get => MucDoUuTienKH_Filter;
            set => MucDoUuTienKH_Filter = value;
        }

        // text hiển thị
        public string? TinhTrangCuaKH_Text { get; set; }
        public string? MucDoUuTienKH_Text { get; set; }
        public string? TrangThaiThucHienKH_Text { get; set; }
    }

    public class ResultFindKeHoachSX : KeHoachSXBase
    {
        public string? KieuKH { get; set; }
        public string? DoUuTien { get; set; }
        public string? TrangThaiDon { get; set; }
    }


}

#nullable restore
