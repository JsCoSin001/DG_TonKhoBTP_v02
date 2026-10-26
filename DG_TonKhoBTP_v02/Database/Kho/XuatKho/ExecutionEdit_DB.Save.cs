using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal sealed class ExecutionEditException : Exception
    {
        internal string Key { get; }
        internal bool Conflict { get; }
        internal ExecutionEditException(string key, string message, bool conflict = false) : base(message)
        { Key = key; Conflict = conflict; }
    }

    internal static partial class ExecutionEdit_DB
    {
        internal static int Exec(SQLiteConnection conn, SQLiteTransaction tx, string sql, params object[] parameters)
        {
            using var cmd = new SQLiteCommand(sql, conn, tx);
            for (int i = 0; i < parameters.Length; i += 2)
                cmd.Parameters.AddWithValue((string)parameters[i], parameters[i + 1] ?? DBNull.Value);
            return cmd.ExecuteNonQuery();
        }
        private static string Fingerprint(ExecutionEditRequest r)
        {
            var payload = new { r.Query, Versions = r.ExpectedVersions.OrderBy(x => x.Key).ToArray(),
                Rows = r.RowEdits.OrderBy(x => x.OperationToken).ToArray(), Reels = r.ReelEdits.OrderBy(x => x.ReelId).ToArray() };
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(ExecutionEditAudit.Json(payload)))).Replace("-", "");
        }
        internal static void ValidateActor(SQLiteConnection conn, SQLiteTransaction tx)
            => ValidateActor(conn, tx, UserContext.UserId, UserContext.UserName, UserContext.IsAuthenticated);
        private static void ValidateActor(SQLiteConnection conn, SQLiteTransaction tx, int actorId, string actorName, bool authenticated)
        {
            if (!authenticated || Table(conn, tx, @"SELECT user_id FROM users
                WHERE user_id=@id AND username=@name AND is_active=1", "@id", actorId, "@name", actorName).Rows.Count != 1)
                throw new ExecutionEditException("actor", "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
            if (Table(conn, tx, "SELECT name FROM sqlite_master WHERE type='table' AND name='KhoaDatabase'").Rows.Count != 0)
            {
                var config = Table(conn, tx, "SELECT Active FROM KhoaDatabase ORDER BY ID DESC LIMIT 1");
                if (config.Rows.Count != 0 && N(config.Rows[0], "Active") == 0)
                    throw new ExecutionEditException("maintenance", "Dữ liệu đang được khóa theo cấu hình hệ thống.");
            }
        }
        internal static ExecutionDetailSnapshot ValidateRequest(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest r)
        {
            var s = Load(conn, tx, r.Query);
            if (!s.EditingEnabled) throw new ExecutionEditException("plan", s.PlanStatus == "HUY" ?
                "Kế hoạch đã hủy chỉ được xem." : "Chức năng sửa chưa được bật sau khi cập nhật B2/B3.");
            if (!r.ExpectedVersions.TryGetValue("FACTORY", out long version) || version != s.Versions["FACTORY"])
                throw new ExecutionEditException("version", "Dữ liệu đã thay đổi. Bản nháp được giữ lại; vui lòng đối chiếu dữ liệu mới.", true);
            if (r.RowEdits.Count == 0 && r.ReelEdits.Count == 0) throw new ExecutionEditException("request", "Chưa có thay đổi để lưu.");
            if (r.RowEdits.Select(x => x.OperationToken).Distinct().Count() != r.RowEdits.Count ||
                r.ReelEdits.Select(x => x.ReelId).Distinct().Count() != r.ReelEdits.Count)
                throw new ExecutionEditException("request", "Một đối tượng chỉ có một thay đổi trong lần lưu.");
            foreach (var edit in r.RowEdits)
            {
                var row = s.Rows.SingleOrDefault(x => x.Done && x.OperationToken.Length > 0 && x.OperationToken == edit.OperationToken);
                if (row == null) throw new ExecutionEditException(edit.OperationToken, "Không tìm thấy lần thực hiện trong phạm vi đang xem.", true);
                if (edit.Undo)
                {
                    if (edit.Reason != "MISTAKE") throw new ExecutionEditException(row.RowKey,
                        "Hoàn tác chỉ áp dụng khi ghi nhận nhầm và thực tế chưa lấy/chưa cắt.");
                    if (row.ReelId.HasValue && r.ReelEdits.Any(x => x.ReelId == row.ReelId.Value))
                        throw new ExecutionEditException(row.RowKey, "Chọn hoàn tác hoặc sửa mốc cuộn trong một lần lưu; không thực hiện cả hai.");
                }
                else if (edit.Reason != "INFO" || !s.Users.Any(x => x.Username == edit.NewPerformerUsername))
                    throw new ExecutionEditException(row.RowKey, "Chọn tài khoản có trong hệ thống và lý do sửa người thực hiện.");
            }
            foreach (var edit in r.ReelEdits)
            {
                if (!s.Reels.TryGetValue(edit.ReelId, out var reel))
                    throw new ExecutionEditException("REEL:" + edit.ReelId, "Cuộn không thuộc phạm vi đang xem.");
                long start = (long)edit.NewEnd - (long)edit.NewDirection * reel.Length;
                if (edit.Reason != "MARKERS" || (edit.NewDirection != 1 && edit.NewDirection != -1) ||
                    edit.NewEnd < 0 || start < 0 || start > int.MaxValue || reel.Status != "ACTIVE" || reel.Length <= 0)
                    throw new ExecutionEditException("REEL:" + edit.ReelId, "Mốc không hợp lệ hoặc cuộn đã hết dây. Chiều dài tồn phải giữ nguyên.");
                if (Table(conn, tx, "SELECT ThuTu FROM B3ThaoTac WHERE TonCuonLe_ID=@id AND Loai='CUT' AND TrangThai='ACTIVE' LIMIT 1",
                    "@id", edit.ReelId).Rows.Count == 0)
                    throw new ExecutionEditException("REEL:" + edit.ReelId, "Chỉ sửa mốc cuộn đã có thực hiện B3.");
            }
            return s;
        }
        internal static void Audit(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest request,
            string kind, string entity, long id, string? token, string? basis, long? markerId, string reason,
            string note, object before, object after)
        {
            Exec(conn, tx, @"INSERT INTO B2DieuChinhSuKien
                (MaLanLuu,Loai,DoiTuong,DoiTuong_ID,MaThaoTac,MaThaoTacCoSo,SuKienMoc_ID,LyDo,GhiChu,TruocJson,SauJson)
                VALUES(@batch,@kind,@entity,@id,@token,@basis,@marker,@reason,@note,@before,@after)",
                "@batch", request.ChangeId.ToString("D"), "@kind", kind, "@entity", entity, "@id", id,
                "@token", (object?)token ?? DBNull.Value, "@basis", (object?)basis ?? DBNull.Value,
                "@marker", (object?)markerId ?? DBNull.Value, "@reason", reason, "@note", note ?? "",
                "@before", ExecutionEditAudit.Json(before), "@after", ExecutionEditAudit.Json(after));
        }
        public static ExecutionEditResult Save(ExecutionEditRequest request)
        {
            if (request == null || request.Query == null || request.ChangeId == Guid.Empty)
                return Error("request", "Yêu cầu lưu không hợp lệ.");
            int actorId = UserContext.UserId;
            string actorName = UserContext.UserName;
            bool authenticated = UserContext.IsAuthenticated;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var conn = DB_Base.OpenConnection(); conn.DefaultTimeout = 2;
                    using var tx = conn.BeginTransaction(false);
                    EnsureSchema(conn, tx); ValidateActor(conn, tx, actorId, actorName, authenticated);
                    string fingerprint = Fingerprint(request);
                    var previous = Table(conn, tx, "SELECT * FROM B2DieuChinhLanLuu WHERE MaLanLuu=@id", "@id", request.ChangeId.ToString("D"));
                    if (previous.Rows.Count != 0)
                    {
                        if (T(previous.Rows[0], "DauVanTayYeuCau") != fingerprint || N(previous.Rows[0], "NguoiSua_ID") != actorId)
                            throw new ExecutionEditException("request", "Mã lần lưu đã được dùng cho nội dung hoặc tài khoản khác.");
                        var retry = new ExecutionEditResult { Success = true, ChangedPlanIds = new List<long> { request.Query.PlanId } };
                        try { retry.NewSnapshot = Load(conn, tx, request.Query); } catch (InvalidOperationException) { }
                        tx.Commit(); return retry;
                    }
                    var s = ValidateRequest(conn, tx, request);
                    var undoPlan = ExecutionUndoPlanner.Build(request, UndoContext(conn, tx, s));
                    Exec(conn, tx, @"INSERT INTO B2DieuChinhLanLuu
                        (MaLanLuu,DauVanTayYeuCau,KeHoach_ID,NguoiSua_ID,NguoiSua,KetQuaJson)
                        VALUES(@id,@hash,@plan,@actorId,@actor,@result)",
                        "@id", request.ChangeId.ToString("D"), "@hash", fingerprint, "@plan", request.Query.PlanId,
                        "@actorId", actorId, "@actor", actorName,
                        "@result", ExecutionEditAudit.Json(new { version = 1, plan_id = request.Query.PlanId, success = true }));
                    ApplyCorrections(conn, tx, request, s);
                    ApplyUndo(conn, tx, request, undoPlan);
                    ValidateInventory(conn, tx, s, request, undoPlan);
                    var result = new ExecutionEditResult { Success = true, NewSnapshot = Load(conn, tx, request.Query),
                        ChangedPlanIds = new List<long> { request.Query.PlanId }, ChangedReelIds = request.ReelEdits.Select(x => x.ReelId)
                            .Concat(undoPlan.ReelRestores.Select(x => x.ReelId)).Distinct().ToList() };
                    tx.Commit(); return result;
                }
                catch (ExecutionEditException ex) { return Error(ex.Key, ex.Message, ex.Conflict); }
                catch (SQLiteException ex) when ((ex.ResultCode == SQLiteErrorCode.Busy || ex.ResultCode == SQLiteErrorCode.Locked) && attempt < 2)
                { Thread.Sleep(100 * (attempt + 1)); }
                catch (SQLiteException ex)
                {
                    System.Diagnostics.Trace.TraceError("Execution edit transaction: {0}", ex);
                    return Error("database", "Không thể lưu thay đổi. Không có dữ liệu nào được ghi; vui lòng thử lại.");
                }
                catch (InvalidOperationException ex) { return Error("data", ex.Message); }
                catch (OverflowException) { return Error("markers", "Mốc số vượt giới hạn cho phép."); }
            }
            return Error("database", "Cơ sở dữ liệu đang bận. Vui lòng thử lại.");
        }
        private static void ApplyCorrections(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest r, ExecutionDetailSnapshot s)
        {
            foreach (var edit in r.RowEdits.Where(x => !x.Undo))
            {
                var row = s.Rows.Single(x => x.OperationToken == edit.OperationToken);
                string table = row.Kind == "TAKE" ? "LichSuLayCuon" : "LichSuCat";
                var before = Table(conn, tx, "SELECT * FROM " + table + " WHERE id=@id", "@id", row.HistoryId).Rows[0];
                Exec(conn, tx, "UPDATE " + table + " SET NguoiThucHien=@user WHERE id=@id", "@user", edit.NewPerformerUsername, "@id", row.HistoryId);
                var after = Table(conn, tx, "SELECT * FROM " + table + " WHERE id=@id", "@id", row.HistoryId).Rows[0];
                Audit(conn, tx, r, "DOI_NGUOI", row.Kind, row.HistoryId, row.OperationToken, null, null, edit.Reason, edit.Note,
                    ExecutionEditAudit.Row(before), ExecutionEditAudit.Row(after));
            }
            foreach (var edit in r.ReelEdits)
            {
                var reel = s.Reels[edit.ReelId];
                int start = checked((int)((long)edit.NewEnd - edit.NewDirection * reel.Length));
                var before = Table(conn, tx, "SELECT * FROM TonCuonLe WHERE id=@id", "@id", edit.ReelId).Rows[0];
                string basis = T(Table(conn, tx, @"SELECT MaThaoTac FROM B3ThaoTac WHERE TonCuonLe_ID=@id
                    AND Loai='CUT' AND TrangThai='ACTIVE' ORDER BY ThuTu DESC LIMIT 1", "@id", edit.ReelId).Rows[0], "MaThaoTac");
                Exec(conn, tx, "UPDATE TonCuonLe SET SoDau=@start,SoCuoi=@end WHERE id=@id",
                    "@start", start, "@end", edit.NewEnd, "@id", edit.ReelId);
                var after = Table(conn, tx, "SELECT * FROM TonCuonLe WHERE id=@id", "@id", edit.ReelId).Rows[0];
                Audit(conn, tx, r, "DOI_MOC", "REEL", edit.ReelId, null, basis, null, edit.Reason, edit.Note,
                    ExecutionEditAudit.Row(before), ExecutionEditAudit.Row(after));
            }
        }
        internal static ExecutionEditResult Error(string key, string message, bool conflict = false) => new ExecutionEditResult {
            Conflict = conflict, ErrorsByKey = new Dictionary<string, string> { [key] = message } };
    }
}
