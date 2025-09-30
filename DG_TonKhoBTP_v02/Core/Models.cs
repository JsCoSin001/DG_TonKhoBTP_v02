// File: Core/Models.cs
// Mục đích: Các DTO tối thiểu để đóng gói snapshot theo cấu trúc DB đã có.
// Tham chiếu cột theo file SQL (CaiDatCDBoc, TTNVL, ThongTinCaLamViec, ...).
// [Luồng 3] Snapshot gom dữ liệu từ nhiều UC vào một biến.

using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Core
{
    // Khớp bảng TTNVL (cột nào không nhập thì lưu 0)
    public class TTNVL
    {
        public int Id { get; set; }                 // Optional (nếu có)
        public int ThongTinSP_ID { get; set; }      // Optional (chưa gán lúc nhập tay)
        public string BinNVL { get; set; }          // NOT NULL trong DB
        public double ConLai { get; set; }
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
        public DateTime Ngay { get; set; }
        public string May { get; set; }
        public string Ca { get; set; }
        public string NguoiLam { get; set; }
        public string ToTruong { get; set; }
        public string QuanDoc { get; set; }
    }

    // Tổ hợp snapshot toàn form (linh hoạt)
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
