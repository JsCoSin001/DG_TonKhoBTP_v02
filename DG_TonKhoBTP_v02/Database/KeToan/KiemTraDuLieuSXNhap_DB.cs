using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class KiemTraDuLieuSXNhap_DB
    {
        public static List<DanhSachLoiNhapLieuSX_Model> LayDanhSachChuaXacNhan()
        {
            var result = new List<DanhSachLoiNhapLieuSX_Model>();

            const string sql = @"
            SELECT
                loi.id                                      AS IdLoi,
                loi.TTThanhpham_id                          AS TTThanhPhamId,
                tp.CongDoan                                 AS CongDoanId,
                IFNULL(tp.MaBin, '')                        AS LotThanhPham,
                IFNULL(ca.Ngay, '')                         AS Ngay,
                IFNULL(ca.May, '')                          AS May,
                IFNULL(ca.Ca, '')                           AS Ca,
                IFNULL(ca.NguoiLam, '')                     AS NguoiLam,
                IFNULL(dsp.Ten, '')                         AS TenThanhPham,
                IFNULL(loi.NoiDungLoi, '')                  AS NoiDungLoi,
                IFNULL(loi.Confirmed, 0)                    AS Confirmed
            FROM DanhSachLoiNhapLieuSX loi
            LEFT JOIN TTThanhPham tp
                   ON loi.TTThanhpham_id = tp.id
            LEFT JOIN DanhSachMaSP dsp
                   ON tp.DanhSachSP_ID = dsp.id
            LEFT JOIN ThongTinCaLamViec ca
                   ON ca.id = (
                       SELECT caMoiNhat.id
                       FROM ThongTinCaLamViec caMoiNhat
                       WHERE caMoiNhat.TTThanhPham_id = loi.TTThanhpham_id
                       ORDER BY caMoiNhat.id DESC
                       LIMIT 1
                   )
            WHERE IFNULL(loi.Confirmed, 0) = 0
            ORDER BY
                CASE
                    WHEN IFNULL(ca.Ngay, '') = '' THEN 1
                    ELSE 0
                END ASC,
                ca.Ngay ASC,
                IFNULL(dsp.Ten, '') COLLATE NOCASE ASC,
                loi.id ASC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int? congDoanId = GetNullableInt(reader, "CongDoanId");

                    result.Add(new DanhSachLoiNhapLieuSX_Model
                    {
                        IdLoi = GetInt(reader, "IdLoi"),
                        TTThanhPhamId = GetInt(reader, "TTThanhPhamId"),
                        LotThanhPham = GetString(reader, "LotThanhPham"),
                        Ngay = DinhDangNgay(GetString(reader, "Ngay")),
                        May = GetString(reader, "May"),
                        Ca = GetString(reader, "Ca"),
                        NguoiLam = GetString(reader, "NguoiLam"),
                        CongDoanId = congDoanId,
                        TenCongDoan = congDoanId.HasValue
                            ? ThongTinChungCongDoan.GetTenCongDoanById(congDoanId.Value)
                            : string.Empty,
                        TenThanhPham = GetString(reader, "TenThanhPham"),
                        NoiDungLoi = GetString(reader, "NoiDungLoi"),
                        Confirmed = GetInt(reader, "Confirmed") == 1
                    });
                }
            }

            return result;
        }

        public static bool CapNhatConfirmed(int idLoi, bool confirmed)
        {
            const string sql = @"
                UPDATE DanhSachLoiNhapLieuSX
                SET Confirmed = @Confirmed
                WHERE id = @IdLoi;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Confirmed", confirmed ? 1 : 0);
                cmd.Parameters.AddWithValue("@IdLoi", idLoi);

                return cmd.ExecuteNonQuery() == 1;
            }
        }

        public static List<ChiTietLoiNhapLieuSX_Model> LayChiTietBomVaNguyenLieuThucTe(int ttThanhPhamId)
        {
            int? danhSachSanPhamId = LayDanhSachSanPhamId(ttThanhPhamId);
            if (!danhSachSanPhamId.HasValue)
            {
                return new List<ChiTietLoiNhapLieuSX_Model>();
            }

            List<BomItem> bomItems = LayDanhSachBom(danhSachSanPhamId.Value);
            List<ThucTeItem> thucTeItems = LayDanhSachNguyenLieuThucTe(ttThanhPhamId);

            return GhepBomVaThucTe(bomItems, thucTeItems);
        }

        private static int? LayDanhSachSanPhamId(int ttThanhPhamId)
        {
            const string sql = @"
                SELECT DanhSachSP_ID
                FROM TTThanhPham
                WHERE id = @TTThanhPhamId
                LIMIT 1;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TTThanhPhamId", ttThanhPhamId);
                object value = cmd.ExecuteScalar();

                if (value == null || value == DBNull.Value)
                {
                    return null;
                }

                return Convert.ToInt32(value);
            }
        }

        private static List<BomItem> LayDanhSachBom(int parentProductId)
        {
            var result = new List<BomItem>();

            // Không lọc theo CongDoan hoặc Active theo yêu cầu nghiệp vụ.
            const string sql = @"
                SELECT
                    bom.Component                      AS ComponentId,
                    IFNULL(dsp.Ten, '')                AS TenNLBom
                FROM BOMStructure bom
                LEFT JOIN DanhSachMaSP dsp
                       ON bom.Component = dsp.id
                WHERE bom.ParentProduct = @ParentProductId
                ORDER BY IFNULL(dsp.Ten, '') COLLATE NOCASE ASC,
                         bom.Component ASC,
                         bom.id ASC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ParentProductId", parentProductId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new BomItem
                        {
                            ComponentId = GetNullableInt(reader, "ComponentId"),
                            TenNLBom = GetString(reader, "TenNLBom")
                        });
                    }
                }
            }

            return result;
        }

        private static List<ThucTeItem> LayDanhSachNguyenLieuThucTe(int ttThanhPhamId)
        {
            var result = new List<ThucTeItem>();

            const string sql = @"
                SELECT
                    nvl.id                                  AS TTNVLId,
                    nvl.DanhSachMaSP_ID                     AS DanhSachMaSPId,
                    IFNULL(dsp.Ten, '')                     AS TenNLThucTe,
                    IFNULL(nvl.BinNVL, '')                  AS LotThucTe
                FROM TTNVL nvl
                LEFT JOIN DanhSachMaSP dsp
                       ON nvl.DanhSachMaSP_ID = dsp.id
                WHERE nvl.TTThanhPham_ID = @TTThanhPhamId
                ORDER BY IFNULL(dsp.Ten, '') COLLATE NOCASE ASC,
                         IFNULL(nvl.BinNVL, '') COLLATE NOCASE ASC,
                         nvl.id ASC;";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TTThanhPhamId", ttThanhPhamId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ThucTeItem
                        {
                            TTNVLId = GetInt(reader, "TTNVLId"),
                            DanhSachMaSPId = GetNullableInt(reader, "DanhSachMaSPId"),
                            TenNLThucTe = GetString(reader, "TenNLThucTe"),
                            LotThucTe = GetString(reader, "LotThucTe")
                        });
                    }
                }
            }

            return result;
        }

        private static List<ChiTietLoiNhapLieuSX_Model> GhepBomVaThucTe(
            List<BomItem> bomItems,
            List<ThucTeItem> thucTeItems)
        {
            var result = new List<ChiTietLoiNhapLieuSX_Model>();
            var matchedTTNVLIds = new HashSet<int>();

            foreach (BomItem bom in bomItems)
            {
                List<ThucTeItem> matchedItems = bom.ComponentId.HasValue
                    ? thucTeItems
                        .Where(x => x.DanhSachMaSPId.HasValue &&
                                    x.DanhSachMaSPId.Value == bom.ComponentId.Value)
                        .ToList()
                    : new List<ThucTeItem>();

                if (matchedItems.Count == 0)
                {
                    result.Add(new ChiTietLoiNhapLieuSX_Model
                    {
                        ComponentId = bom.ComponentId,
                        TenNLBom = bom.TenNLBom
                    });
                    continue;
                }

                // Mỗi LOT thực tế là một dòng riêng.
                foreach (ThucTeItem actual in matchedItems)
                {
                    matchedTTNVLIds.Add(actual.TTNVLId);

                    result.Add(new ChiTietLoiNhapLieuSX_Model
                    {
                        ComponentId = bom.ComponentId,
                        TenNLBom = bom.TenNLBom,
                        DanhSachMaSPThucTeId = actual.DanhSachMaSPId,
                        TenNLThucTe = actual.TenNLThucTe,
                        LotThucTe = actual.LotThucTe
                    });
                }
            }

            // Các nguyên liệu thực tế không có trong BOM vẫn phải được hiển thị.
            foreach (ThucTeItem actual in thucTeItems.Where(x => !matchedTTNVLIds.Contains(x.TTNVLId)))
            {
                result.Add(new ChiTietLoiNhapLieuSX_Model
                {
                    DanhSachMaSPThucTeId = actual.DanhSachMaSPId,
                    TenNLThucTe = actual.TenNLThucTe,
                    LotThucTe = actual.LotThucTe
                });
            }

            return result
                .OrderBy(x => string.IsNullOrWhiteSpace(x.TenNLBom) ? x.TenNLThucTe : x.TenNLBom,
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.TenNLThucTe, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.LotThucTe, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string DinhDangNgay(string ngay)
        {
            if (string.IsNullOrWhiteSpace(ngay))
            {
                return string.Empty;
            }

            DateTime ngayLamViec;
            return DateTime.TryParseExact(
                ngay.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out ngayLamViec)
                ? ngayLamViec.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : ngay;
        }

        private static string GetString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal)
                ? string.Empty
                : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int GetInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal)
                ? 0
                : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? GetNullableInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal)
                ? (int?)null
                : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private sealed class BomItem
        {
            public int? ComponentId { get; set; }
            public string TenNLBom { get; set; }
        }

        private sealed class ThucTeItem
        {
            public int TTNVLId { get; set; }
            public int? DanhSachMaSPId { get; set; }
            public string TenNLThucTe { get; set; }
            public string LotThucTe { get; set; }
        }
    }
}
