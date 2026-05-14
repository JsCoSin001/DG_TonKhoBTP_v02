using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan;

namespace DG_TonKhoBTP_v02.Database.KeToan
{
    public static class BoSungThongTinSP_DB
    {
        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM TÊN SP CHO COMBOBOX (dùng cho ComboBoxSearchHelper)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm kiếm bất đồng bộ trong DanhSachMaSP theo Ten hoặc Ten_KhongDau.
        /// Chỉ lấy sản phẩm có KieuSP = 'TP' và chưa có bản ghi trong TTBoSung.
        /// DisplayColumn của ComboBoxSearchHelper nên đặt là "Ten".
        /// </summary>
        public static Task<DataTable> TimKiemTenSPAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                const string sql = @"
                    SELECT  id,
                            Ten,
                            Ten_KhongDau,
                            Ma
                    FROM    DanhSachMaSP
                    WHERE   KieuSP = 'TP'
                      AND   (Ten          LIKE @keyword
                          OR Ten_KhongDau LIKE @keyword)
                      AND   NOT EXISTS (
                                SELECT 1
                                FROM   TTBoSung bs
                                WHERE  bs.DanhSachMaSP_ID = DanhSachMaSP.id
                            )
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
        // TÌM KIẾM BẢNG GHI TTBoSung THEO TÊN SP
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tìm bản ghi TTBoSung đầu tiên khớp tên SP (LIKE, không phân biệt hoa thường).
        /// Is và No được escape bằng ngoặc vuông vì là từ khoá reserved trong SQLite.
        /// Trả về BoSungThongTinSP_Model nếu tìm thấy, null nếu không.
        /// </summary>
        public static BoSungThongTinSP_Model TimKiemBoSungTheoTenSP(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            // Lưu ý: bs.[Is] và bs.[No] phải dùng ngoặc vuông vì là reserved keyword.
            // Dùng alias col_Is / col_No để MapReader đọc an toàn.
            const string sql = @"
                SELECT  bs.id,
                        bs.DanhSachMaSP_ID,
                        sp.Ma,
                        sp.Ten,
                        bs.TenChiTiet,
                        bs.TieuChuan,
                        bs.Rph,
                        bs.Rt,
                        bs.Rtd,
                        bs.Rcd,
                        bs.Ca,
                        bs.[Is]   AS col_Is,
                        bs.Istt,
                        bs.Sh,
                        bs.[No]   AS col_No,
                        bs.D,
                        bs.TNC,
                        bs.OD,
                        bs.T,
                        bs.M,
                        bs.KC,
                        bs.LTDN,
                        bs.GhiChu,
                        bs.LH
                FROM    TTBoSung      bs
                JOIN    DanhSachMaSP  sp ON sp.id = bs.DanhSachMaSP_ID
                WHERE   sp.Ten          LIKE @keyword COLLATE NOCASE
                     OR sp.Ten_KhongDau LIKE @keyword COLLATE NOCASE
                ORDER BY bs.id DESC
                LIMIT 1";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return MapReader(reader);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // INSERT
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Thêm mới một bản ghi TTBoSung. Trả về id vừa tạo.</summary>
        public static long InsertBoSung(BoSungThongTinSP_Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            const string sql = @"
                INSERT INTO TTBoSung
                    (DanhSachMaSP_ID, TenChiTiet, TieuChuan,
                     Rph, Rt, Rtd, Rcd, Ca,
                     [Is], Istt, Sh, [No], D,
                     TNC, OD, T, M, KC, LTDN, GhiChu, LH)
                VALUES
                    (@DanhSachMaSP_ID, @TenChiTiet, @TieuChuan,
                     @Rph, @Rt, @Rtd, @Rcd, @Ca,
                     @Is, @Istt, @Sh, @No, @D,
                     @TNC, @OD, @T, @M, @KC, @LTDN, @GhiChu, @LH);
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

        // ════════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Cập nhật bản ghi TTBoSung theo Id. Ném ngoại lệ nếu không tìm thấy.</summary>
        public static void UpdateBoSung(BoSungThongTinSP_Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Id <= 0) throw new ArgumentException("Id không hợp lệ.", nameof(model));

            const string sql = @"
                UPDATE TTBoSung SET
                    DanhSachMaSP_ID = @DanhSachMaSP_ID,
                    TenChiTiet      = @TenChiTiet,
                    TieuChuan       = @TieuChuan,
                    Rph             = @Rph,
                    Rt              = @Rt,
                    Rtd             = @Rtd,
                    Rcd             = @Rcd,
                    Ca              = @Ca,
                    [Is]            = @Is,
                    Istt            = @Istt,
                    Sh              = @Sh,
                    [No]            = @No,
                    D               = @D,
                    TNC             = @TNC,
                    OD              = @OD,
                    T               = @T,
                    M               = @M,
                    KC              = @KC,
                    LTDN            = @LTDN,
                    GhiChu          = @GhiChu,
                    LH              = @LH
                WHERE id = @Id";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                BindParams(cmd, model);
                cmd.Parameters.AddWithValue("@Id", model.Id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    throw new InvalidOperationException(
                        $"Không tìm thấy bản ghi TTBoSung id={model.Id} để cập nhật.");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Xoá bản ghi TTBoSung theo id. Ném ngoại lệ nếu không tìm thấy.</summary>
        public static void DeleteBoSung(long id)
        {
            if (id <= 0) throw new ArgumentException("Id không hợp lệ.", nameof(id));

            const string sql = "DELETE FROM TTBoSung WHERE id = @id";

            using (var conn = DB_Base.OpenConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    throw new InvalidOperationException(
                        $"Không tìm thấy bản ghi TTBoSung id={id} để xoá.");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS NỘI BỘ
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Gắn tất cả tham số từ model vào SQLiteCommand (dùng chung INSERT/UPDATE).</summary>
        private static void BindParams(SQLiteCommand cmd, BoSungThongTinSP_Model m)
        {
            cmd.Parameters.AddWithValue("@DanhSachMaSP_ID", m.DanhSachMaSP_ID);
            cmd.Parameters.AddWithValue("@TenChiTiet", NullIfEmpty(m.TenChiTiet));
            cmd.Parameters.AddWithValue("@TieuChuan", NullIfEmpty(m.TieuChuan));
            cmd.Parameters.AddWithValue("@Rph", NullIfEmpty(m.Rph));
            cmd.Parameters.AddWithValue("@Rt", NullIfEmpty(m.Rt));
            cmd.Parameters.AddWithValue("@Rtd", NullIfEmpty(m.Rtd));
            cmd.Parameters.AddWithValue("@Rcd", NullIfEmpty(m.Rcd));
            cmd.Parameters.AddWithValue("@Ca", NullIfEmpty(m.Ca));
            cmd.Parameters.AddWithValue("@Is", NullIfEmpty(m.Is));
            cmd.Parameters.AddWithValue("@Istt", NullIfEmpty(m.Istt));
            cmd.Parameters.AddWithValue("@Sh", NullIfEmpty(m.Sh));
            cmd.Parameters.AddWithValue("@No", NullIfEmpty(m.No));
            cmd.Parameters.AddWithValue("@D", NullIfEmpty(m.D));
            cmd.Parameters.AddWithValue("@TNC", NullIfEmpty(m.TNC));
            cmd.Parameters.AddWithValue("@OD", NullIfEmpty(m.OD));
            cmd.Parameters.AddWithValue("@T", NullIfEmpty(m.T));
            cmd.Parameters.AddWithValue("@M", NullIfEmpty(m.M));
            cmd.Parameters.AddWithValue("@KC", NullIfEmpty(m.KC));
            cmd.Parameters.AddWithValue("@LTDN", NullIfEmpty(m.LTDN));
            cmd.Parameters.AddWithValue("@GhiChu", NullIfEmpty(m.GhiChu));
            cmd.Parameters.AddWithValue("@LH", NullIfEmpty(m.LH));
        }

        /// <summary>
        /// Ánh xạ một dòng SQLiteDataReader sang BoSungThongTinSP_Model.
        /// Is/No đọc qua alias col_Is/col_No (đặt trong SELECT của TimKiemBoSungTheoTenSP).
        /// </summary>
        private static BoSungThongTinSP_Model MapReader(SQLiteDataReader r)
        {
            return new BoSungThongTinSP_Model
            {
                Id = Convert.ToInt64(r["id"]),
                DanhSachMaSP_ID = Convert.ToInt64(r["DanhSachMaSP_ID"]),
                Ma = Str(r, "Ma"),
                Ten = Str(r, "Ten"),
                TenChiTiet = Str(r, "TenChiTiet"),
                TieuChuan = Str(r, "TieuChuan"),
                Rph = Str(r, "Rph"),
                Rt = Str(r, "Rt"),
                Rtd = Str(r, "Rtd"),
                Rcd = Str(r, "Rcd"),
                Ca = Str(r, "Ca"),
                Is = Str(r, "col_Is"),   // alias vì Is là reserved keyword
                Istt = Str(r, "Istt"),
                Sh = Str(r, "Sh"),
                No = Str(r, "col_No"),   // alias vì No là reserved keyword
                D = Str(r, "D"),
                TNC = Str(r, "TNC"),
                OD = Str(r, "OD"),
                T = Str(r, "T"),
                M = Str(r, "M"),
                KC = Str(r, "KC"),
                LTDN = Str(r, "LTDN"),
                GhiChu = Str(r, "GhiChu"),
                LH = Str(r, "LH"),
            };
        }

        private static object NullIfEmpty(string s) =>
            string.IsNullOrWhiteSpace(s) ? (object)DBNull.Value : s.Trim();

        private static string Str(SQLiteDataReader r, string col) =>
            r[col] == DBNull.Value ? string.Empty : r[col]?.ToString() ?? string.Empty;
    }
}