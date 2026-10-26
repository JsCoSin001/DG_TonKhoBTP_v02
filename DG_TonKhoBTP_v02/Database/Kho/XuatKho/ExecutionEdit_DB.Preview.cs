using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static partial class ExecutionEdit_DB
    {
        internal static ExecutionUndoContext UndoContext(SQLiteConnection conn, SQLiteTransaction tx, ExecutionDetailSnapshot snapshot)
        {
            var context = new ExecutionUndoContext { Snapshot = snapshot };
            var reels = new HashSet<long>(snapshot.Reels.Keys);
            var visibleTokens = new HashSet<string>(snapshot.Rows.Select(x => x.OperationToken));
            foreach (DataRow row in Table(conn, tx, "SELECT * FROM B3ThaoTac WHERE TrangThai='ACTIVE' ORDER BY ThuTu").Rows)
            {
                long? reel = NullableId(row, "TonCuonLe_ID"); string token = T(row, "MaThaoTac");
                if (!visibleTokens.Contains(token) && (!reel.HasValue || !reels.Contains(reel.Value))) continue;
                context.Operations.Add(new ExecutionOperation { Sequence = N(row, "ThuTu"), Token = token, Kind = T(row, "Loai"),
                    HistoryId = N(row, "LichSu_ID"), PlanId = N(row, "KeHoach_ID"), ItemId = N(row, "KeHoachHang_ID"),
                    ReelId = reel, SourceId = N(row, "TTCuonDay_ID"), Qr = T(row, "RawQr"),
                    Before = ExecutionEditAudit.Parse(T(row, "TruocJson")), After = ExecutionEditAudit.Parse(T(row, "SauJson")) });
            }
            foreach (long reel in reels)
            {
                var r = Table(conn, tx, "SELECT * FROM TonCuonLe WHERE id=@id", "@id", reel);
                if (r.Rows.Count == 1) context.CurrentReels.Add(reel, ExecutionEditAudit.Row(r.Rows[0]));
                foreach (DataRow row in Table(conn, tx, "SELECT * FROM LichSuCat WHERE TonCuonLe_ID=@id", "@id", reel).Rows)
                    context.LiveCuts.Add(N(row, "id"), ExecutionEditAudit.Row(row));
                foreach (DataRow row in Table(conn, tx, @"SELECT n.id,h.KeHoach_ID FROM KeHoachCatNhom n
                    JOIN KeHoachHang h ON h.id=n.KeHoachHang_ID WHERE n.TonCuonLe_ID=@id", "@id", reel).Rows)
                    context.Groups.Add(new ExecutionGroupLink { GroupId = N(row, "id"), PlanId = N(row, "KeHoach_ID"), ReelId = reel });
                foreach (DataRow row in Table(conn, tx, @"SELECT s.*,b.KeHoach_ID,b.NguoiSua,b.ThoiGianSua,k.MaKeHoach
                    FROM B2DieuChinhSuKien s JOIN B2DieuChinhLanLuu b ON b.MaLanLuu=s.MaLanLuu
                    LEFT JOIN KeHoach k ON k.id=b.KeHoach_ID WHERE s.Loai='DOI_MOC'
                    AND s.DoiTuong='REEL' AND s.DoiTuong_ID=@id AND NOT EXISTS
                    (SELECT 1 FROM B2DieuChinhSuKien h WHERE h.Loai='HET_HIEU_LUC_MOC' AND h.SuKienMoc_ID=s.id)", "@id", reel).Rows)
                    context.MarkerEvents.Add(new ExecutionMarkerEvent { Id = N(row, "id"), ReelId = reel,
                        BasisToken = T(row, "MaThaoTacCoSo"), AfterJson = T(row, "SauJson"), BeforeJson = T(row, "TruocJson"),
                        PlanId = N(row, "KeHoach_ID"), PlanCode = T(row, "MaKeHoach"), Editor = T(row, "NguoiSua"), EditedAt = T(row, "ThoiGianSua") });
            }
            foreach (var op in context.Operations.Where(x => x.Kind == "TAKE"))
            {
                var r = Table(conn, tx, "SELECT * FROM LichSuLayCuon WHERE id=@id", "@id", op.HistoryId);
                if (r.Rows.Count == 1) context.LiveTakes[op.HistoryId] = ExecutionEditAudit.Row(r.Rows[0]);
            }
            foreach (DataRow r in Table(conn, tx, "SELECT * FROM B3QrSuDung").Rows)
                context.QrUsages.Add(ExecutionEditAudit.Row(r));
            return context;
        }
        public static ExecutionEditPreview Preview(ExecutionEditRequest request)
        {
            try
            {
                using var conn = DB_Base.OpenConnection(); using var tx = conn.BeginTransaction(true);
                EnsureSchema(conn, tx); ValidateActor(conn, tx);
                var snapshot = ValidateRequest(conn, tx, request);
                var plan = ExecutionUndoPlanner.Build(request, UndoContext(conn, tx, snapshot));
                var preview = new ExecutionEditPreview { Valid = true, UndoPlan = plan };
                foreach (var restore in plan.ReelRestores)
                {
                    string impact = restore.Remove ? "Hoàn trả 1 cuộn chẵn; gỡ cuộn lẻ phát sinh và đồng bộ QR/nhóm cắt." :
                        "Khôi phục mốc " + restore.Start + " → " + restore.End + "; tồn " + Math.Abs((long)restore.End - restore.Start) + " m.";
                    int superseded = plan.MarkerEventsToSupersede.Count(x => x.ReelId == restore.ReelId);
                    if (superseded > 0) impact += " Có " + superseded + " lần sửa mốc phụ thuộc không còn áp dụng (vẫn giữ nhật ký).";
                    foreach (var marker in plan.MarkerEventsToSupersede.Where(x => x.ReelId == restore.ReelId))
                    {
                        var before = ExecutionEditAudit.Parse(marker.BeforeJson); var after = ExecutionEditAudit.Parse(marker.AfterJson);
                        impact += Environment.NewLine + "Sửa mốc #" + marker.Id + " · " + (marker.PlanCode.Length > 0 ? marker.PlanCode : "Kế hoạch #" + marker.PlanId) +
                            " · " + marker.Editor + " · " + marker.EditedAt + ": " + ExecutionEditAudit.Number(before, "SoDau") + " → " + ExecutionEditAudit.Number(before, "SoCuoi") +
                            " thành " + ExecutionEditAudit.Number(after, "SoDau") + " → " + ExecutionEditAudit.Number(after, "SoCuoi") + " sẽ hết hiệu lực.";
                    }
                    preview.ImpactByReelId[restore.ReelId] = "Cuộn " + snapshot.Reels[restore.ReelId].Code + ": " + impact;
                }
                foreach (var op in plan.UndoOperations)
                    preview.ImpactByRowKey[snapshot.Rows.Single(x => x.OperationToken == op.Token).RowKey] =
                        "Đưa về Chưa thực hiện" + (op.Kind == "TAKE" ? "; hoàn trả 1 cuộn và đồng bộ QR." : "; khôi phục theo chuỗi trên cuộn.");
                foreach (var edit in request.ReelEdits)
                    preview.ImpactByReelId[edit.ReelId] = "Sửa mốc hiện tại; giữ nguyên tồn " + snapshot.Reels[edit.ReelId].Length + " m.";
                tx.Commit(); return preview;
            }
            catch (ExecutionEditException ex) { return new ExecutionEditPreview { ErrorsByKey = new Dictionary<string, string> { [ex.Key] = ex.Message } }; }
            catch (InvalidOperationException ex) { return new ExecutionEditPreview { ErrorsByKey = new Dictionary<string, string> { ["data"] = ex.Message } }; }
            catch (SQLiteException) { return new ExecutionEditPreview { ErrorsByKey = new Dictionary<string, string> { ["database"] = "Không thể kiểm tra dữ liệu mới nhất. Vui lòng thử lại." } }; }
            catch (ArgumentException) { return new ExecutionEditPreview { ErrorsByKey = new Dictionary<string, string> { ["data"] = "Nhật ký nguồn không hợp lệ, không được suy đoán để sửa." } }; }
        }
    }
}
