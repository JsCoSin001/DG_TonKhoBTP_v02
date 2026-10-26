using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static partial class ExecutionEdit_DB
    {
        internal static DataTable Table(SQLiteConnection conn, SQLiteTransaction? tx, string sql, params object[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, conn, tx);
            for (int i = 0; i < parameters.Length; i += 2)
                cmd.Parameters.AddWithValue((string)parameters[i], parameters[i + 1] ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            // Joined projections contain nullable history IDs for pending segments.
            // Do not import the source table's PK/non-null constraints into this view.
            var table = new DataTable();
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            while (reader.Read())
            {
                var values = new object[reader.FieldCount]; reader.GetValues(values); table.Rows.Add(values);
            }
            return table;
        }
        internal static long N(DataRow row, string name) => row[name] == DBNull.Value ? 0 : Convert.ToInt64(row[name], CultureInfo.InvariantCulture);
        internal static string T(DataRow row, string name) => row[name] == DBNull.Value ? "" : Convert.ToString(row[name], CultureInfo.InvariantCulture) ?? "";
        internal static long? NullableId(DataRow row, string name) => row[name] == DBNull.Value ? (long?)null : N(row, name);
        internal static void EnsureSchema(SQLiteConnection conn, SQLiteTransaction? tx)
        {
            if (Table(conn, tx, "SELECT name FROM sqlite_master WHERE type='table' AND name='B3TinhNangDuLieu'").Rows.Count == 0 ||
                Table(conn, tx, "SELECT PhienBan FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT' AND PhienBan=1").Rows.Count != 1)
                throw new InvalidOperationException("Cần cập nhật cấu trúc dữ liệu B2/B3 trước khi sửa chi tiết.");
        }
        public static ExecutionDetailSnapshot Load(ExecutionDetailQuery query)
        {
            using var conn = DB_Base.OpenConnection();
            using var tx = conn.BeginTransaction(true);
            var snapshot = Load(conn, tx, query); tx.Commit(); return snapshot;
        }
        internal static ExecutionDetailSnapshot Load(SQLiteConnection conn, SQLiteTransaction tx, ExecutionDetailQuery query)
        {
            EnsureSchema(conn, tx);
            if (query == null || query.PlanId <= 0 || query.ItemId <= 0 || query.SpecificationId.HasValue == query.PartialReelId.HasValue)
                throw new InvalidOperationException("Phạm vi xem chi tiết không hợp lệ.");
            var plan = Table(conn, tx, @"SELECT k.TrangThai FROM KeHoach k JOIN KeHoachHang h ON h.KeHoach_ID=k.id
                WHERE k.id=@plan AND h.id=@item", "@plan", query.PlanId, "@item", query.ItemId);
            if (plan.Rows.Count != 1) throw new InvalidOperationException("Dòng không còn thuộc kế hoạch đang xem.");
            var s = new ExecutionDetailSnapshot { Query = query, PlanStatus = T(plan.Rows[0], "TrangThai") };
            s.EditingEnabled = s.PlanStatus == "ACTIVE" && N(Table(conn, tx,
                "SELECT ChoPhepSua FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT'").Rows[0], "ChoPhepSua") == 1;
            if (query.SpecificationId.HasValue)
            {
                var qc = Table(conn, tx, "SELECT * FROM KeHoachHangQuyCachCuonChan WHERE id=@qc AND KeHoachHang_ID=@item",
                    "@qc", query.SpecificationId.Value, "@item", query.ItemId);
                if (qc.Rows.Count != 1) throw new InvalidOperationException("Quy cách không còn thuộc dòng kế hoạch.");
                var takes = Table(conn, tx, @"SELECT h.*,o.MaThaoTac,o.RawQr,p.MaBin AS SourceLabel FROM LichSuLayCuon h
                    LEFT JOIN B3ThaoTac o ON o.Loai='TAKE' AND o.LichSu_ID=h.id AND o.TrangThai='ACTIVE'
                    JOIN TTCuonDay c ON c.id=h.TTCuonDay_ID JOIN TTNhapKhoTP n ON n.id=c.ThongTinNhapKho_ID JOIN TTThanhPham p ON p.id=n.TTThanhPham_ID
                    WHERE h.QuyCachCuonChan_ID=@qc AND h.KeHoachHang_ID=@item ORDER BY h.id",
                    "@qc", query.SpecificationId.Value, "@item", query.ItemId);
                int taken = 0;
                foreach (DataRow r in takes.Rows)
                {
                    int count = checked((int)N(r, "SoLuong")); taken = checked(taken + count);
                    s.Rows.Add(new ExecutionDetailRow { RowKey = "TAKE:" + N(r, "id"), Kind = "TAKE", Done = true,
                        OperationToken = T(r, "MaThaoTac"), HistoryId = N(r, "id"), Quantity = count,
                        Length = checked((int)N(qc.Rows[0], "ChieuDai1Cuon")), PerformerUsername = T(r, "NguoiThucHien"),
                        PerformedAt = T(r, "ThoiGianThucHien"), RawQr = T(r, "RawQr"), SourceId = N(r, "TTCuonDay_ID"), SourceLabel = T(r, "SourceLabel") });
                }
                int pending = checked((int)N(qc.Rows[0], "SoLuongCuonCanLay")) - taken;
                if (pending > 0) s.Rows.Add(new ExecutionDetailRow { RowKey = "PENDING_TAKE", Kind = "TAKE", Quantity = pending,
                    Length = checked((int)N(qc.Rows[0], "ChieuDai1Cuon")) });
            }
            else if (Table(conn, tx, @"SELECT id FROM KeHoachCatNhom WHERE KeHoachHang_ID=@item AND TonCuonLe_ID=@reel",
                "@item", query.ItemId, "@reel", query.PartialReelId!.Value).Rows.Count == 0)
                throw new InvalidOperationException("Cuộn không còn thuộc nhóm kế hoạch đang xem.");

            var cuts = Table(conn, tx, @"SELECT d.id AS DetailId,n.id AS GroupId,n.TonCuonLe_ID AS PlannedReel,
                d.ChieuDaiCanCat,h.*,o.MaThaoTac,t.MaCuon
                FROM KeHoachCatNhom n JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID=n.id
                LEFT JOIN LichSuCat h ON h.KeHoachCatChiTiet_ID=d.id
                LEFT JOIN B3ThaoTac o ON o.Loai='CUT' AND o.LichSu_ID=h.id AND o.TrangThai='ACTIVE'
                LEFT JOIN TonCuonLe t ON t.id=COALESCE(h.TonCuonLe_ID,n.TonCuonLe_ID)
                WHERE n.KeHoachHang_ID=@item AND ((@qc IS NOT NULL AND n.QuyCachCuonChan_ID=@qc)
                    OR (@reel IS NOT NULL AND n.TonCuonLe_ID=@reel)) ORDER BY n.id,d.id",
                "@item", query.ItemId, "@qc", (object?)query.SpecificationId ?? DBNull.Value,
                "@reel", (object?)query.PartialReelId ?? DBNull.Value);
            foreach (DataRow r in cuts.Rows)
            {
                bool done = r["id"] != DBNull.Value;
                long? reel = done ? NullableId(r, "TonCuonLe_ID") : NullableId(r, "PlannedReel");
                s.Rows.Add(new ExecutionDetailRow { RowKey = "CUT:" + N(r, "DetailId"), Kind = "CUT", Done = done,
                    DetailId = N(r, "DetailId"), GroupId = N(r, "GroupId"), ReelId = reel, HistoryId = done ? N(r, "id") : 0,
                    OperationToken = T(r, "MaThaoTac"), Quantity = 1, Length = checked((int)N(r, "ChieuDaiCanCat")),
                    PerformerUsername = T(r, "NguoiThucHien"), PerformedAt = T(r, "ThoiGianThucHien"),
                    RawQr = done ? T(r, "MaQuetThucTe") : T(r, "MaCuon"), SourceKind = done ? T(r, "LoaiNguon") : reel.HasValue ? "CUON_LE" : "CUON_CHAN",
                    BeforeStart = checked((int)N(r, "SoDauTruoc")), BeforeEnd = checked((int)N(r, "SoCuoiTruoc")),
                    CutStart = checked((int)N(r, "SoDauCat")), CutEnd = checked((int)N(r, "SoCuoiCat")),
                    AfterStart = checked((int)N(r, "SoDauSau")), AfterEnd = checked((int)N(r, "SoCuoiSau")) });
                if (reel.HasValue && !s.Reels.ContainsKey(reel.Value))
                {
                    var current = Table(conn, tx, @"SELECT t.*,p.MaBin AS SourceLabel FROM TonCuonLe t
                        JOIN TTThanhPham p ON p.id=t.TTThanhPham_ID WHERE t.id=@id", "@id", reel.Value);
                    if (current.Rows.Count != 1) throw new InvalidOperationException("Cuộn thực tế đã thay đổi. Vui lòng tải lại.");
                    var cr = current.Rows[0];
                    s.Reels.Add(reel.Value, new ExecutionReelState { ReelId = reel.Value, Code = T(cr, "MaCuon"), SourceId = N(cr, "TTCuonDay_ID"), SourceLabel = T(cr, "SourceLabel"),
                        Start = checked((int)N(cr, "SoDau")), End = checked((int)N(cr, "SoCuoi")), Status = T(cr, "TrangThai") });
                }
            }
            foreach (DataRow r in Table(conn, tx, "SELECT Khoa,GiaTri FROM B3PhienBanDuLieu").Rows)
                s.Versions.Add(T(r, "Khoa"), N(r, "GiaTri"));
            foreach (DataRow r in Table(conn, tx, "SELECT user_id,username,is_active FROM users ORDER BY username COLLATE NOCASE").Rows)
                s.Users.Add(new ExecutionPerformer { UserId = N(r, "user_id"), Username = T(r, "username"), IsActive = N(r, "is_active") == 1 });
            return s;
        }
        public static DataTable LoadAudit(long planId, long? reelId, string operationToken)
        {
            using var conn = DB_Base.OpenConnection(); EnsureSchema(conn, null);
            return Table(conn, null, @"SELECT s.*,b.NguoiSua,b.NguoiSua_ID,b.ThoiGianSua FROM B2DieuChinhSuKien s
                JOIN B2DieuChinhLanLuu b ON b.MaLanLuu=s.MaLanLuu
                WHERE b.KeHoach_ID=@plan AND ((@reel IS NOT NULL AND s.DoiTuong='REEL' AND s.DoiTuong_ID=@reel)
                  OR (@token<>'' AND s.MaThaoTac=@token) OR (@reel IS NULL AND @token='')) ORDER BY s.id DESC",
                "@plan", planId, "@reel", (object?)reelId ?? DBNull.Value, "@token", operationToken ?? "");
        }
        public static long ReadPlanRevision(long planId)
        {
            using var conn = DB_Base.OpenConnection();
            if (Table(conn, null, "SELECT name FROM sqlite_master WHERE name='B3PhienBanDuLieu' AND type='table'").Rows.Count == 0) return 0;
            return N(Table(conn, null, "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'").Rows[0], "GiaTri");
        }
        internal static long ReadRevision(SQLiteConnection conn, SQLiteTransaction tx)
            => N(Table(conn, tx, "SELECT GiaTri FROM B3PhienBanDuLieu WHERE Khoa='FACTORY'").Rows[0], "GiaTri");
        internal static bool PlanningRevisionRequired(SQLiteConnection conn, SQLiteTransaction tx)
        {
            if (Table(conn, tx, "SELECT name FROM sqlite_master WHERE name='B3TinhNangDuLieu' AND type='table'").Rows.Count == 0) return false;
            var rows = Table(conn, tx, "SELECT ChoPhepSua FROM B3TinhNangDuLieu WHERE Ten='EXECUTION_EDIT'").Rows;
            return rows.Count == 1 && N(rows[0], "ChoPhepSua") == 1;
        }
    }
}
