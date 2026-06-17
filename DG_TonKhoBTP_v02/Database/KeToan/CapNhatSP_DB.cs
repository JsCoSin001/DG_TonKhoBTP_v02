using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models.KeToan;
using System;
using System.Data;
using System.Data.SQLite;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public class CapNhatSP_DB
    {
        public static int InsertDSMaSP(CapNhatSP_Model sp)
        {
            if (sp == null) throw new ArgumentNullException(nameof(sp));

            const string sql = @"
                INSERT INTO DanhSachMaSP
                (Ten, Ten_KhongDau, Ma, DonVi, KieuSP, ChuyenDoi, Active, DateInsert)
                VALUES
                (@Ten, @Ten_KhongDau, @Ma, @DonVi, @KieuSP, @ChuyenDoi, @Active, @DateInsert);";

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                BindDanhSachMaSPParams(cmd, sp);
                cmd.ExecuteNonQuery();

                using var getIdCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, tx);
                int finalId = Convert.ToInt32(getIdCmd.ExecuteScalar());

                tx.Commit();
                return finalId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static int UpdateDanhSachMaSP(CapNhatSP_Model sp, int id)
        {
            if (sp == null) throw new ArgumentNullException(nameof(sp));

            const string sql = @"
                UPDATE DanhSachMaSP
                SET
                    Ten = @Ten,
                    Ten_KhongDau = @Ten_KhongDau,
                    Ma = @Ma,
                    DonVi = @DonVi,
                    KieuSP = @KieuSP,
                    ChuyenDoi = @ChuyenDoi,
                    Active = @Active,
                    DateInsert = @DateInsert
                WHERE id = @Id;";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(sql, conn);

            BindDanhSachMaSPParams(cmd, sp);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                throw new Exception($"Không tìm thấy bản ghi ID = {id}");

            return id;
        }

        public static int UpsertDanhSachMaSP(CapNhatSP_Model sp, string idText)
        {
            bool isInsert = string.IsNullOrWhiteSpace(idText);
            if (isInsert) return InsertDSMaSP(sp);

            if (!int.TryParse(idText.Trim(), out int id))
                throw new Exception("ID KHÔNG HỢP LỆ.");

            return UpdateDanhSachMaSP(sp, id);
        }

        public static DataTable GetDanhSachMaSP(int loaiSP, string kieuSP)
        {
            string sql = @"
                SELECT id as STT, Ma, Ten, DonVi, ChuyenDoi, Active
                FROM DanhSachMaSP";

            if (loaiSP != 3 && loaiSP >= 0)
            {
                sql += " WHERE KieuSP = @KieuSP";
                sql += " ORDER BY id DESC";
                return DB_Base.GetData(sql, kieuSP, "KieuSP");
            }

            sql += " ORDER BY id DESC";
            return DB_Base.GetData(sql);
        }

        public static DataTable TimDanhSachLuaChon(int loaiTimKiem, string keyword)
        {
            string table;
            string cot;
            string key = keyword ?? string.Empty;

            switch (loaiTimKiem)
            {
                case 0: // Mã SP
                    table = "DanhSachMaSP";
                    cot = "Ma";
                    key = CoreHelper.BoDauTiengViet(key);
                    break;

                case 1: // Tên SP
                    table = "DanhSachMaSP";
                    cot = "Ten";
                    key = CoreHelper.BoDauTiengViet(key);
                    break;

                case 2: // Nhà cung cấp
                    table = "DanhSachNCC";
                    cot = "TenNCC_KhongDau";
                    key = CoreHelper.BoDauTiengViet(key);
                    break;

                case 3: // Kho
                    table = "DanhSachKho";
                    cot = "TenKho_KhongDau";
                    key = CoreHelper.BoDauTiengViet(key);
                    break;

                case 4: // Rulo
                    table = "TTLo";
                    cot = "KichThuoc";
                    break;

                default:
                    table = "DanhSachMaSP";
                    cot = "Ma";
                    key = CoreHelper.BoDauTiengViet(key);
                    break;
            }

            string sql = $@"
                SELECT *
                FROM {table}
                WHERE {cot} LIKE '%' || @Key || '%' COLLATE NOCASE
                ORDER BY id DESC";

            return DB_Base.GetData(sql, key, "Key");
        }

        public static string GetDisplayMemberLuaChon(int loaiTimKiem)
        {
            switch (loaiTimKiem)
            {
                case 0: return "Ma";
                case 1: return "Ten";
                case 2: return "TenNCC";
                case 3: return "TenKho";
                case 4: return "KichThuoc";
                default: return "Ma";
            }
        }

        public static int UpsertDanhSachNCC(string id, string ma, string tenNcc, string diaChi = null)
        {
            var model = new CapNhatSP_Model.NhaCungCap_Model
            {
                Id = TryParseNullableInt(id),
                Ma = ma,
                TenNCC = tenNcc,
                TenNCC_KhongDau = string.IsNullOrWhiteSpace(tenNcc)
                    ? null
                    : CoreHelper.BoDauTiengViet(tenNcc.Trim()),
                DiaChi = diaChi
            };

            return UpsertDanhSachNCC(model);
        }

        public static int UpsertDanhSachNCC(CapNhatSP_Model.NhaCungCap_Model ncc)
        {
            if (ncc == null) throw new ArgumentNullException(nameof(ncc));

            bool isInsert = !ncc.Id.HasValue;

            string sql = @"
                INSERT INTO DanhSachNCC
                (Ma, TenNCC, TenNCC_KhongDau, DiaChi)
                VALUES
                (@Ma, @TenNCC, @TenNCC_KhongDau, @DiaChi);";

            if (!isInsert)
            {
                sql = @"
                    UPDATE DanhSachNCC
                    SET
                        Ma = @Ma,
                        TenNCC = @TenNCC,
                        TenNCC_KhongDau = @TenNCC_KhongDau,
                        DiaChi = @DiaChi
                    WHERE id = @id;";
            }

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);

                cmd.Parameters.AddWithValue("@Ma", ToDbString(ncc.Ma));
                cmd.Parameters.AddWithValue("@TenNCC", ToDbString(ncc.TenNCC));
                cmd.Parameters.AddWithValue("@TenNCC_KhongDau", ToDbString(ncc.TenNCC_KhongDau));
                cmd.Parameters.AddWithValue("@DiaChi", ToDbString(ncc.DiaChi));

                if (!isInsert)
                    cmd.Parameters.AddWithValue("@id", ncc.Id.Value);

                cmd.ExecuteNonQuery();

                int finalId;
                if (isInsert)
                {
                    using var getIdCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, tx);
                    finalId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                }
                else
                {
                    finalId = ncc.Id.Value;
                }

                tx.Commit();
                return finalId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static int UpsertDanhSachKho(string id, string kiHieu, string tenKho, string ghiChu = null)
        {
            var model = new CapNhatSP_Model.Kho_Model
            {
                Id = TryParseNullableInt(id),
                KiHieu = kiHieu,
                TenKho = tenKho,
                TenKho_KhongDau = string.IsNullOrWhiteSpace(tenKho)
                    ? null
                    : CoreHelper.BoDauTiengViet(tenKho.Trim()),
                GhiChu = ghiChu
            };

            return UpsertDanhSachKho(model);
        }

        public static int UpsertDanhSachKho(CapNhatSP_Model.Kho_Model kho)
        {
            if (kho == null) throw new ArgumentNullException(nameof(kho));

            bool isInsert = !kho.Id.HasValue;

            string sql = @"
                INSERT INTO DanhSachKho
                (KiHieu, TenKho, TenKho_KhongDau, GhiChu)
                VALUES
                (@KiHieu, @TenKho, @TenKho_KhongDau, @GhiChu);";

            if (!isInsert)
            {
                sql = @"
                    UPDATE DanhSachKho
                    SET
                        KiHieu = @KiHieu,
                        TenKho = @TenKho,
                        TenKho_KhongDau = @TenKho_KhongDau,
                        GhiChu = @GhiChu
                    WHERE id = @id;";
            }

            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);

                cmd.Parameters.AddWithValue("@KiHieu", ToDbString(kho.KiHieu));
                cmd.Parameters.AddWithValue("@TenKho", ToDbString(kho.TenKho));
                cmd.Parameters.AddWithValue("@TenKho_KhongDau", ToDbString(kho.TenKho_KhongDau));
                cmd.Parameters.AddWithValue("@GhiChu", ToDbString(kho.GhiChu));

                if (!isInsert)
                    cmd.Parameters.AddWithValue("@id", kho.Id.Value);

                cmd.ExecuteNonQuery();

                int finalId;
                if (isInsert)
                {
                    using var getIdCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, tx);
                    finalId = Convert.ToInt32(getIdCmd.ExecuteScalar());
                }
                else
                {
                    finalId = kho.Id.Value;
                }

                tx.Commit();
                return finalId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static void BindDanhSachMaSPParams(SQLiteCommand cmd, CapNhatSP_Model sp)
        {
            cmd.Parameters.AddWithValue("@Ten", ToDbString(sp.Ten));
            cmd.Parameters.AddWithValue("@Ten_KhongDau", ToDbString(sp.Ten_KhongDau));
            cmd.Parameters.AddWithValue("@Ma", ToDbString(sp.Ma));
            cmd.Parameters.AddWithValue("@DonVi", ToDbString(sp.DonVi));
            cmd.Parameters.AddWithValue("@KieuSP", ToDbString(sp.KieuSP));
            cmd.Parameters.AddWithValue("@ChuyenDoi", sp.ChuyenDoi);
            cmd.Parameters.AddWithValue("@Active", sp.Active ? 1 : 0);
            cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert ?? DateTime.Now);
        }

        private static object ToDbString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static int? TryParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (!int.TryParse(value.Trim(), out int id))
                throw new Exception("ID KHÔNG HỢP LỆ.");

            return id;
        }
    }
}
