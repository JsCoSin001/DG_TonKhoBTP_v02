using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    /// <summary>Các truy vấn chỉ đọc của báo cáo tháng, dùng kết nối chung DB_Base.</summary>
    internal static class MonthyReport_DB
    {
        // Một lần duyệt ca kết thúc trong tháng; chọn ca kết thúc mới nhất của mỗi thành phẩm.
        // Không JOIN TTNVL: một thành phẩm nhiều NVL vẫn chỉ có một dòng báo cáo.
        private const string ReportSql = @"
            SELECT ttp.id AS Id, clv.NgayKetThuc AS NgayKetThuc,
                   clv.CaKetThuc AS CaKetThuc, clv.May AS May,
                   clv.NguoiKetThuc AS NguoiKetThuc,
                   ttp.MaBin AS MaBin, sp.Ten AS TenSanPham,
                   ttp.GhiChu AS GhiChu,
                   ttp.KhoiLuongSau AS KhoiLuongSau,
                   ttp.ChieuDaiSau AS ChieuDaiSau, sp.DonVi AS DonVi,
                   ttp.Temp AS Temp
            FROM TTThanhPham ttp
            JOIN (
                SELECT c.*, ROW_NUMBER() OVER (
                    PARTITION BY c.TTThanhPham_id
                    ORDER BY c.NgayKetThuc DESC, c.id DESC
                ) AS ShiftOrder
                FROM ThongTinCaLamViec c
                WHERE c.NgayKetThuc >= @start AND c.NgayKetThuc < @end
            ) clv ON clv.TTThanhPham_id = ttp.id AND clv.ShiftOrder = 1
            LEFT JOIN DanhSachMaSP sp ON sp.id = ttp.DanhSachSP_ID
            WHERE ttp.CongDoan = @congDoan
            ORDER BY clv.NgayKetThuc DESC, ttp.id DESC";

        private const string MaterialSql = @"
            SELECT nvl.id AS Id, nvl.TTThanhPham_ID AS TTThanhPhamId,
                   nvl.BinNVL AS BinNVL, sp.Ma AS MaNVL, sp.Ten AS TenNVL,
                   sp.DonVi AS DonVi, nvl.KlBatDau AS KlBatDau,
                   nvl.KlConLai AS KlConLai, nvl.CdBatDau AS CdBatDau,
                   nvl.CdConLai AS CdConLai, nvl.QC AS QC,
                   nvl.DuongKinhSoiDong AS DuongKinhSoiDong,
                   nvl.SoSoi AS SoSoi, nvl.KetCauLoi AS KetCauLoi,
                   nvl.DuongKinhSoiMach AS DuongKinhSoiMach,
                   nvl.BanRongBang AS BanRongBang, nvl.DoDayBang AS DoDayBang
            FROM TTNVL nvl
            LEFT JOIN DanhSachMaSP sp ON sp.id = nvl.DanhSachMaSP_ID
            WHERE nvl.TTThanhPham_ID = @id ORDER BY nvl.id ASC";

        private const string ShiftSql = @"
            SELECT clv.id AS Id, clv.TTThanhPham_id AS TTThanhPhamId,
                   clv.May AS May, clv.Ca AS Ca, clv.NguoiLam AS NguoiLam,
                   clv.NgayBatDau AS NgayBatDau, clv.GioBatDau AS GioBatDau,
                   clv.NgayKetThuc AS NgayKetThuc, clv.GioKetThuc AS GioKetThuc,
                   clv.CaKetThuc AS CaKetThuc, clv.NguoiKetThuc AS NguoiKetThuc,
                   clv.ToTruong AS ToTruong, clv.QuanDoc AS QuanDoc
            FROM ThongTinCaLamViec clv
            WHERE clv.TTThanhPham_id = @id
            ORDER BY clv.id ASC";

        public static List<MonthyReport_Model> GetMonthlyReport(DateTime selectedMonth, int congDoanId)
        {
            var start = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var end = start.AddMonths(1);
            var result = new List<MonthyReport_Model>();
            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(ReportSql, conn))
            {
                cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                // TTThanhPham.CongDoan có kiểu TEXT trong SQLite hiện tại.
                cmd.Parameters.AddWithValue("@congDoan", congDoanId.ToString(CultureInfo.InvariantCulture));
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MonthyReport_Model
                        {
                            Id = Convert.ToInt64(reader["Id"], CultureInfo.InvariantCulture),
                            NgayKetThuc = DateValue(reader, "NgayKetThuc"),
                            CaKetThuc = TextValue(reader, "CaKetThuc"),
                            May = TextValue(reader, "May"),
                            NguoiKetThuc = TextValue(reader, "NguoiKetThuc"),
                            MaBin = TextValue(reader, "MaBin"),
                            TenSanPham = TextValue(reader, "TenSanPham"),
                            GhiChu = TextValue(reader, "GhiChu"),
                            KhoiLuongSau = NumberValue(reader, "KhoiLuongSau"),
                            ChieuDaiSau = NumberValue(reader, "ChieuDaiSau"),
                            DonVi = TextValue(reader, "DonVi"),
                            Temp = IntValue(reader, "Temp"),
                            CongDoanId = congDoanId
                        });
                    }
                }
            }
            return result;
        }

        public static List<MonthyReport_MaterialModel> GetMaterialDetails(long ttThanhPhamId)
        {
            var result = new List<MonthyReport_MaterialModel>();
            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(MaterialSql, conn))
            {
                cmd.Parameters.AddWithValue("@id", ttThanhPhamId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MonthyReport_MaterialModel
                        {
                            Id = Convert.ToInt64(reader["Id"], CultureInfo.InvariantCulture),
                            TTThanhPhamId = Convert.ToInt64(reader["TTThanhPhamId"], CultureInfo.InvariantCulture),
                            BinNVL = TextValue(reader, "BinNVL"),
                            MaNVL = TextValue(reader, "MaNVL"),
                            TenNVL = TextValue(reader, "TenNVL"),
                            DonVi = TextValue(reader, "DonVi"),
                            KlBatDau = NumberValue(reader, "KlBatDau"),
                            KlConLai = NumberValue(reader, "KlConLai"),
                            CdBatDau = NumberValue(reader, "CdBatDau"),
                            CdConLai = NumberValue(reader, "CdConLai"),
                            QC = TextValue(reader, "QC"),
                            DuongKinhSoiDong = NumberValue(reader, "DuongKinhSoiDong"),
                            SoSoi = NumberValue(reader, "SoSoi"),
                            KetCauLoi = NumberValue(reader, "KetCauLoi"),
                            DuongKinhSoiMach = NumberValue(reader, "DuongKinhSoiMach"),
                            BanRongBang = NumberValue(reader, "BanRongBang"),
                            DoDayBang = NumberValue(reader, "DoDayBang")
                        });
                    }
                }
            }
            return result;
        }

        public static List<MonthyReport_ShiftModel> GetShiftDetails(long ttThanhPhamId)
        {
            var result = new List<MonthyReport_ShiftModel>();
            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(ShiftSql, conn))
            {
                cmd.Parameters.AddWithValue("@id", ttThanhPhamId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MonthyReport_ShiftModel
                        {
                            Id = Convert.ToInt64(reader["Id"], CultureInfo.InvariantCulture),
                            TTThanhPhamId = Convert.ToInt64(reader["TTThanhPhamId"], CultureInfo.InvariantCulture),
                            May = TextValue(reader, "May"),
                            Ca = TextValue(reader, "Ca"),
                            NguoiLam = TextValue(reader, "NguoiLam"),
                            NgayBatDau = TextValue(reader, "NgayBatDau"),
                            GioBatDau = TextValue(reader, "GioBatDau"),
                            NgayKetThuc = TextValue(reader, "NgayKetThuc"),
                            GioKetThuc = TextValue(reader, "GioKetThuc"),
                            CaKetThuc = TextValue(reader, "CaKetThuc"),
                            NguoiKetThuc = TextValue(reader, "NguoiKetThuc"),
                            ToTruong = TextValue(reader, "ToTruong"),
                            QuanDoc = TextValue(reader, "QuanDoc")
                        });
                    }
                }
            }
            return result;
        }

        private static string? TextValue(IDataRecord reader, string name)
        {
            int index = reader.GetOrdinal(name);
            return reader.IsDBNull(index) ? null : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static double? NumberValue(IDataRecord reader, string name)
        {
            int index = reader.GetOrdinal(name);
            return reader.IsDBNull(index) ? (double?)null : Convert.ToDouble(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static int? IntValue(IDataRecord reader, string name)
        {
            int index = reader.GetOrdinal(name);
            return reader.IsDBNull(index) ? (int?)null : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime? DateValue(IDataRecord reader, string name)
        {
            string? text = TextValue(reader, name);
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (DateTime.TryParseExact(text, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed)) return parsed;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) return parsed;
            return null;
        }
    }
}
