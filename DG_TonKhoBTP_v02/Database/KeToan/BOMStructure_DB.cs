using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models.KeToan;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class BOMStructure_DB
    {
        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM SẢN PHẨM CHO COMBOBOX BOM
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm thành phẩm trong DanhSachMaSP theo Ten, Ten_KhongDau hoặc Ma.
        /// Không lọc KieuSP, không lọc Active.
        /// </summary>
        public static Task<DataTable> TimKiemThanhPhamAsync(string keyword, CancellationToken ct)
        {
            return TimKiemSanPhamAsync(keyword, ct, onlyBtp: false);
        }

        /// <summary>
        /// Tìm nguyên liệu trong DanhSachMaSP theo Ten, Ten_KhongDau hoặc Ma.
        /// Chỉ lấy KieuSP = 'BTP'. Không lọc Active.
        /// </summary>
        public static Task<DataTable> TimKiemNguyenLieuAsync(string keyword, CancellationToken ct)
        {
            return TimKiemSanPhamAsync(keyword, ct, onlyBtp: true);
        }

        private static Task<DataTable> TimKiemSanPhamAsync(string keyword, CancellationToken ct, bool onlyBtp)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                string sql = @"
                    SELECT  id,
                            Ten,
                            Ten_KhongDau,
                            Ma
                    FROM    DanhSachMaSP
                    WHERE   (Ten          LIKE @keyword COLLATE NOCASE
                          OR Ten_KhongDau LIKE @keyword COLLATE NOCASE
                          OR Ma           LIKE @keyword COLLATE NOCASE)";

                if (onlyBtp)
                    sql += " AND KieuSP = 'BTP'";

                sql += @"
                    ORDER BY Ten
                    LIMIT   50";

                var dt = new DataTable();

                using (var conn = DB_Base.OpenConnection())
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");
                    ct.ThrowIfCancellationRequested();

                    using (var adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(dt);
                }

                return dt;
            }, ct);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOAD / INSERT / UPDATE / SOFT DELETE
        // ════════════════════════════════════════════════════════════════════════

        public static DataTable GetByParentProduct(long parentProduct)
        {
            const string sql = @"
                SELECT  bom.id,
                        bom.ParentProduct,
                        bom.Component,
                        sp.Ma AS ma,
                        sp.Ten AS ten,
                        bom.TyLe AS tyLe,
                        bom.TyLeHoanDoi AS tyLeHoanDoi,
                        bom.CongDoan AS congDoanId,
                        bom.Active AS activeValue
                FROM    BOMStructure bom
                JOIN    DanhSachMaSP sp ON sp.id = bom.Component
                WHERE   bom.ParentProduct = @ParentProduct
                ORDER BY bom.id";

            var dt = new DataTable();

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ParentProduct", parentProduct);
                using (var adapter = new SQLiteDataAdapter(cmd))
                    adapter.Fill(dt);
            }

            EnsureDisplayColumns(dt);

            foreach (DataRow row in dt.Rows)
            {
                int congDoanId = ToInt(row["congDoanId"]);
                int activeValue = ToInt(row["activeValue"]);

                row["congDoan"] = ThongTinChungCongDoan.GetTenCongDoanById(congDoanId);
                row["active"] = ActiveText(activeValue);
                row["colDelete"] = "Xóa";
            }

            return dt;
        }

        public static long Insert(BOMStructure_Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            const string sql = @"
                INSERT INTO BOMStructure
                    (ParentProduct, Component, TyLe, TyLeHoanDoi, CongDoan, Active)
                VALUES
                    (@ParentProduct, @Component, @TyLe, @TyLeHoanDoi, @CongDoan, @Active);
                SELECT last_insert_rowid();";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                BindParams(cmd, model);
                long newId = (long)cmd.ExecuteScalar();
                model.Id = newId;
                return newId;
            }
        }

        public static void Update(BOMStructure_Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Id <= 0) throw new ArgumentException("Id không hợp lệ.", nameof(model));

            const string sql = @"
                UPDATE BOMStructure SET
                    ParentProduct = @ParentProduct,
                    Component     = @Component,
                    TyLe          = @TyLe,
                    TyLeHoanDoi   = @TyLeHoanDoi,
                    CongDoan      = @CongDoan,
                    Active        = @Active
                WHERE id = @Id";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                BindParams(cmd, model);
                cmd.Parameters.AddWithValue("@Id", model.Id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    throw new InvalidOperationException($"Không tìm thấy BOMStructure id={model.Id} để cập nhật.");
            }
        }

        /// <summary>Không xóa vật lý. Chỉ set Active = 0.</summary>
        public static void SoftDelete(long id)
        {
            if (id <= 0) throw new ArgumentException("Id không hợp lệ.", nameof(id));

            const string sql = "UPDATE BOMStructure SET Active = 0 WHERE id = @id";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    throw new InvalidOperationException($"Không tìm thấy BOMStructure id={id} để xoá.");
            }
        }

        public static bool ExistsDuplicate(long parentProduct, long component, int congDoan, long excludeId = 0)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM   BOMStructure
                WHERE  ParentProduct = @ParentProduct
                  AND  Component     = @Component
                  AND  CongDoan      = @CongDoan
                  AND  id           <> @ExcludeId";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ParentProduct", parentProduct);
                cmd.Parameters.AddWithValue("@Component", component);
                cmd.Parameters.AddWithValue("@CongDoan", congDoan);
                cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

                object result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
        }

        public static DataRow CreateGridRow(DataTable table, BOMStructure_Model model)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (model == null) throw new ArgumentNullException(nameof(model));

            EnsureDisplayColumns(table);

            DataRow row = table.NewRow();
            FillGridRow(row, model);
            return row;
        }

        public static void FillGridRow(DataRow row, BOMStructure_Model model)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (model == null) throw new ArgumentNullException(nameof(model));

            row["id"] = model.Id;
            row["ParentProduct"] = model.ParentProduct;
            row["Component"] = model.Component;
            row["ma"] = model.Ma ?? string.Empty;
            row["ten"] = model.Ten ?? string.Empty;
            row["tyLe"] = Convert.ToDouble(model.TyLe);
            row["tyLeHoanDoi"] = Convert.ToDouble(model.TyLeHoanDoi);
            row["congDoanId"] = model.CongDoan;
            row["activeValue"] = model.Active;
            row["congDoan"] = model.TenCongDoan ?? ThongTinChungCongDoan.GetTenCongDoanById(model.CongDoan);
            row["active"] = model.ActiveText ?? ActiveText(model.Active);
            row["colDelete"] = "Xóa";
        }

        private static void BindParams(SQLiteCommand cmd, BOMStructure_Model m)
        {
            cmd.Parameters.AddWithValue("@ParentProduct", m.ParentProduct);
            cmd.Parameters.AddWithValue("@Component", m.Component);
            cmd.Parameters.AddWithValue("@TyLe", m.TyLe);
            cmd.Parameters.AddWithValue("@TyLeHoanDoi", m.TyLeHoanDoi);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan);
            cmd.Parameters.AddWithValue("@Active", m.Active);
        }

        private static void EnsureDisplayColumns(DataTable dt)
        {
            if (!dt.Columns.Contains("ParentProduct")) dt.Columns.Add("ParentProduct", typeof(long));
            if (!dt.Columns.Contains("Component")) dt.Columns.Add("Component", typeof(long));
            if (!dt.Columns.Contains("ma")) dt.Columns.Add("ma", typeof(string));
            if (!dt.Columns.Contains("ten")) dt.Columns.Add("ten", typeof(string));
            if (!dt.Columns.Contains("tyLe")) dt.Columns.Add("tyLe", typeof(decimal));
            if (!dt.Columns.Contains("tyLeHoanDoi")) dt.Columns.Add("tyLeHoanDoi", typeof(decimal));
            if (!dt.Columns.Contains("congDoanId")) dt.Columns.Add("congDoanId", typeof(int));
            if (!dt.Columns.Contains("activeValue")) dt.Columns.Add("activeValue", typeof(int));
            if (!dt.Columns.Contains("congDoan")) dt.Columns.Add("congDoan", typeof(string));
            if (!dt.Columns.Contains("active")) dt.Columns.Add("active", typeof(string));
            if (!dt.Columns.Contains("colDelete")) dt.Columns.Add("colDelete", typeof(string));
        }

        private static string ActiveText(int active) => active == 1 ? "Có" : "Không";

        private static int ToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToInt32(value);
        }
    }
}
