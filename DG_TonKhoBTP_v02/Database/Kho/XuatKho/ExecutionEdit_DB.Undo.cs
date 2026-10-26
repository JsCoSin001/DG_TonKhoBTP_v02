using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static partial class ExecutionEdit_DB
    {
        private static void ApplyUndo(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest request, ExecutionUndoPlan plan)
        {
            foreach (var op in plan.UndoOperations)
            {
                var edit = request.RowEdits.Single(x => x.OperationToken == op.Token);
                string table = op.Kind == "CUT" ? "LichSuCat" : "LichSuLayCuon";
                var before = Table(conn, tx, "SELECT * FROM " + table + " WHERE id=@id", "@id", op.HistoryId).Rows[0];
                Audit(conn, tx, request, "HOAN_TAC", op.Kind, op.HistoryId, op.Token, null, null, edit.Reason, edit.Note,
                    ExecutionEditAudit.Row(before), new { version = 1, done = false, operation_token = op.Token });
                if (op.Kind == "TAKE" && Exec(conn, tx, @"DELETE FROM B3QrSuDung WHERE RawQr=@qr
                    AND LichSuLayCuon_ID=@history AND TrangThai='DA_LAY_NGUYEN'", "@qr", op.Qr, "@history", op.HistoryId) != 1)
                    throw new ExecutionEditException(op.Token, "QR lấy cuộn đã thay đổi.", true);
                if (Exec(conn, tx, "DELETE FROM " + table + " WHERE id=@id", "@id", op.HistoryId) != 1 ||
                    Exec(conn, tx, "UPDATE B3ThaoTac SET TrangThai='HUY' WHERE MaThaoTac=@token AND TrangThai='ACTIVE'", "@token", op.Token) != 1)
                    throw new ExecutionEditException(op.Token, "Thao tác đã được sửa ở nơi khác.", true);
            }
            foreach (var restore in plan.ReelRestores)
            {
                if (restore.Remove)
                {
                    var op = plan.UndoOperations.Single(x => x.Token == restore.FirstToken);
                    if (Exec(conn, tx, @"DELETE FROM B3QrSuDung WHERE TonCuonLe_ID=@id AND RawQr=@qr AND TrangThai='DA_CHUYEN_LE'",
                        "@id", restore.ReelId, "@qr", op.Qr) != 1)
                        throw new ExecutionEditException(op.Token, "QR chuyển lẻ đã thay đổi.", true);
                    if (Exec(conn, tx, "UPDATE KeHoachCatNhom SET TonCuonLe_ID=NULL WHERE id=@group AND TonCuonLe_ID=@reel",
                        "@group", restore.GroupId, "@reel", restore.ReelId) != 1)
                        throw new ExecutionEditException(op.Token, "Liên kết nhóm cắt đã thay đổi.", true);
                    if (Exec(conn, tx, "DELETE FROM TonCuonLe WHERE id=@id", "@id", restore.ReelId) != 1)
                        throw new ExecutionEditException(op.Token, "Cuộn lẻ phát sinh không còn tồn tại.", true);
                }
                else if (Exec(conn, tx, "UPDATE TonCuonLe SET SoDau=@start,SoCuoi=@end,TrangThai=@status WHERE id=@id",
                    "@start", restore.Start, "@end", restore.End, "@status", restore.Status, "@id", restore.ReelId) != 1)
                    throw new ExecutionEditException("REEL:" + restore.ReelId, "Cuộn đã thay đổi.", true);
            }
            foreach (var marker in plan.MarkerEventsToSupersede)
            {
                Audit(conn, tx, request, "HET_HIEU_LUC_MOC", "REEL", marker.ReelId, null, marker.BasisToken, marker.Id,
                    "MISTAKE", "Không còn áp dụng sau khi hoàn tác chuỗi cắt ghi nhận nhầm.",
                    new { marker_id = marker.Id, effective = true }, new { marker_id = marker.Id, effective = false });
            }
        }

        private static void ValidateInventory(SQLiteConnection conn, SQLiteTransaction tx, ExecutionDetailSnapshot snapshot,
            ExecutionEditRequest request, ExecutionUndoPlan plan)
        {
            if (plan.UndoOperations.Count == 0 && request.ReelEdits.Count == 0) return;
            foreach (long reelId in request.ReelEdits.Select(x => x.ReelId).Concat(plan.ReelRestores.Where(x => !x.Remove).Select(x => x.ReelId)).Distinct())
            {
                var reel = Table(conn, tx, @"SELECT t.*, COALESCE((SELECT SUM(d.ChieuDaiCanCat)
                    FROM KeHoachCatNhom n JOIN KeHoachCatChiTiet d ON d.KeHoachCatNhom_ID=n.id
                    JOIN KeHoachHang h ON h.id=n.KeHoachHang_ID JOIN KeHoach k ON k.id=h.KeHoach_ID
                    WHERE n.TonCuonLe_ID=t.id AND k.TrangThai='ACTIVE' AND NOT EXISTS
                    (SELECT 1 FROM LichSuCat l WHERE l.KeHoachCatChiTiet_ID=d.id)),0) AS Reserved
                    FROM TonCuonLe t WHERE t.id=@id", "@id", reelId);
                if (reel.Rows.Count != 1) throw new ExecutionEditException("REEL:" + reelId, "Cuộn tồn không còn hợp lệ.");
                var r = reel.Rows[0]; long length = Math.Abs(N(r, "SoCuoi") - N(r, "SoDau"));
                if (N(r, "SoDau") < 0 || N(r, "SoCuoi") < 0 || length > int.MaxValue || N(r, "Reserved") > length ||
                    T(r, "TrangThai") != (length == 0 ? "HET" : "ACTIVE"))
                    throw new ExecutionEditException("REEL:" + reelId, "Tồn/mốc hoặc đặt trước của cuộn không hợp lệ; không lưu thay đổi.");
            }
            var sources = plan.UndoOperations.Select(x => x.SourceId)
                .Concat(request.ReelEdits.Select(x => snapshot.Reels[x.ReelId].SourceId)).Distinct();
            foreach (long sourceId in sources)
            {
                var identity = Table(conn, tx, @"SELECT p.DanhSachSP_ID,c.ChieuDai_1cuon,c.SoDau,c.SoCuoi FROM TTCuonDay c
                    JOIN TTNhapKhoTP n ON n.id=c.ThongTinNhapKho_ID JOIN TTThanhPham p ON p.id=n.TTThanhPham_ID WHERE c.id=@id",
                    "@id", sourceId);
                if (identity.Rows.Count != 1) throw new ExecutionEditException("SOURCE:" + sourceId, "Nguồn nhập kho không còn hợp lệ.");
                var id = identity.Rows[0];
                // Imported partial reels are checked by physical reel length, not full-reel accounting.
                if (id["SoDau"] != DBNull.Value || id["SoCuoi"] != DBNull.Value) continue;
                long product = N(id, "DanhSachSP_ID"), length = N(id, "ChieuDai_1cuon"), actual = 0;
                var stock = Table(conn, tx, @"SELECT c.id,c.SoCuon,
                    COALESCE((SELECT SUM(l.SoLuong) FROM LichSuLayCuon l WHERE l.TTCuonDay_ID=c.id),0) AS Taken,
                    (SELECT COUNT(DISTINCT t.id) FROM TonCuonLe t JOIN LichSuCat l ON l.TonCuonLe_ID=t.id
                     WHERE t.TTCuonDay_ID=c.id AND l.LoaiNguon='CUON_CHAN') AS Converted
                    FROM TTCuonDay c JOIN TTNhapKhoTP n ON n.id=c.ThongTinNhapKho_ID JOIN TTThanhPham p ON p.id=n.TTThanhPham_ID
                    WHERE p.DanhSachSP_ID=@product AND c.ChieuDai_1cuon=@length AND c.SoDau IS NULL AND c.SoCuoi IS NULL
                      AND p.NhapKho=1 AND p.Temp=0", "@product", product, "@length", length);
                foreach (DataRow r in stock.Rows)
                {
                    long remaining = N(r, "SoCuon") - N(r, "Taken") - N(r, "Converted");
                    if (remaining < 0) throw new ExecutionEditException("SOURCE:" + N(r, "id"), "Tồn cuộn chẵn âm; không lưu thay đổi.");
                    actual = checked(actual + remaining);
                }
                var reserved = Table(conn, tx, @"SELECT
                    COALESCE((SELECT SUM(MAX(0,q.SoLuongCuonCanLay-COALESCE((SELECT SUM(l.SoLuong) FROM LichSuLayCuon l
                        WHERE l.QuyCachCuonChan_ID=q.id),0)))
                        FROM KeHoachHangQuyCachCuonChan q JOIN KeHoachHang h ON h.id=q.KeHoachHang_ID JOIN KeHoach k ON k.id=h.KeHoach_ID
                        WHERE k.TrangThai='ACTIVE' AND h.DanhSachMaSP_ID=@product AND q.ChieuDai1Cuon=@length),0)
                    +(SELECT COUNT(*) FROM KeHoachCatNhom g JOIN KeHoachHangQuyCachCuonChan q ON q.id=g.QuyCachCuonChan_ID
                        JOIN KeHoachHang h ON h.id=g.KeHoachHang_ID JOIN KeHoach k ON k.id=h.KeHoach_ID
                        WHERE k.TrangThai='ACTIVE' AND h.DanhSachMaSP_ID=@product AND q.ChieuDai1Cuon=@length AND g.TonCuonLe_ID IS NULL
                        AND EXISTS(SELECT 1 FROM KeHoachCatChiTiet d WHERE d.KeHoachCatNhom_ID=g.id
                        AND NOT EXISTS(SELECT 1 FROM LichSuCat l WHERE l.KeHoachCatChiTiet_ID=d.id))) AS Reserved",
                    "@product", product, "@length", length);
                if (N(reserved.Rows[0], "Reserved") > actual)
                    throw new ExecutionEditException("SOURCE:" + sourceId, "Đặt trước vượt tồn cuộn chẵn sau xử lý; không lưu thay đổi.");
            }
        }
    }
}
