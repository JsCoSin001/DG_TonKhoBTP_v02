using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal readonly struct CutMarkers : IEquatable<CutMarkers>
    {
        internal int Start { get; }
        internal int End { get; }
        internal long Length => Math.Abs((long)End - Start);
        internal int Direction => Start <= End ? 1 : -1;

        internal CutMarkers(int start, int end) { Start = start; End = end; }
        internal CutMarkers Swap() => new CutMarkers(End, Start);
        internal CutMarkers InDirection(int direction) => Direction == direction ? this : Swap();
        public bool Equals(CutMarkers other) => Start == other.Start && End == other.End;
        public override bool Equals(object? obj) => obj is CutMarkers other && Equals(other);
        public override int GetHashCode() => unchecked(Start * 397 ^ End);
        public override string ToString() => Start + " → " + End;

        internal bool TryCut(int length, out CutMarkers remaining)
        {
            remaining = this;
            if (Start < 0 || End < 0 || length <= 0 || length > Length) return false;
            remaining = new CutMarkers(Start, End - Direction * length);
            return true;
        }

        internal static CutMarkers Read(Dictionary<string, object> data, string suffix = "") => new CutMarkers(
            checked((int)ExecutionEditAudit.Number(data, "SoDau" + suffix)),
            checked((int)ExecutionEditAudit.Number(data, "SoCuoi" + suffix)));

        internal CutMarkers MoveToEnd(int end, int direction, string key)
        {
            long start = end - direction * Length;
            if (end < 0 || start < 0 || start > int.MaxValue)
                throw new ExecutionEditException(key, "Mốc điều chỉnh trong chuỗi cắt vượt giới hạn.");
            return new CutMarkers((int)start, end);
        }

        // Replay a recorded change of numbering over the updated length.
        internal static CutMarkers Replay(CutMarkers original, CutMarkers adjusted, CutMarkers current, string key)
        {
            if (original.Equals(adjusted)) return current;
            if (original.Length != adjusted.Length)
                throw new ExecutionEditException(key, "Chiều dài giữa hai lần cắt không khớp với lịch sử của cuộn.");
            if (original.Swap().Equals(adjusted)) return current.Swap();
            return current.MoveToEnd(adjusted.End, adjusted.Direction, key);
        }
    }

    internal sealed class CorrectedCut
    {
        internal ExecutionOperation Operation { get; set; } = null!;
        internal Dictionary<string, object> Before { get; set; } = null!;
        internal Dictionary<string, object> After { get; set; } = null!;
        internal Dictionary<string, object> History { get; set; } = null!;
        internal CutMarkers BeforeCut { get; set; }
        internal CutMarkers Remaining { get; set; }
        internal string Note { get; set; } = "";
    }

    internal sealed class CutLengthPlan
    {
        internal List<CorrectedCut> Cuts { get; } = new List<CorrectedCut>();
        internal Dictionary<long, CutMarkers> Remaining { get; } = new Dictionary<long, CutMarkers>();
        internal CutMarkers? NextBefore { get; set; }
    }

    internal static partial class ExecutionEdit_DB
    {
        private const string CutLengthReason = "CUT_LENGTH";
        private static readonly string[] CorrectedCutFields = { "ChieuDaiCat", "SoDauTruoc", "SoCuoiTruoc", "SoDauCat",
            "SoCuoiCat", "SoDauSau", "SoCuoiSau", "HeSoChieu", "DieuChinhThongSo" };

        private static void LoadCutCorrections(SQLiteConnection conn, SQLiteTransaction tx, ExecutionUndoContext context)
        {
            var operations = context.Operations.Where(x => x.Kind == "CUT").ToDictionary(x => x.Token);
            if (operations.Count == 0) return;
            foreach (DataRow row in Table(conn, tx, @"SELECT DoiTuong_ID,MaThaoTac,TruocJson,SauJson FROM B2DieuChinhSuKien
                WHERE Loai='DOI_MOC' AND DoiTuong='CUT' AND LyDo=@reason ORDER BY id", "@reason", CutLengthReason).Rows)
            {
                if (!operations.TryGetValue(T(row, "MaThaoTac"), out var op)) continue;
                var prior = ExecutionEditAudit.Parse(T(row, "TruocJson"));
                var effective = ExecutionEditAudit.Parse(T(row, "SauJson"));
                var before = ExecutionEditAudit.Object(effective, "before");
                var after = ExecutionEditAudit.Object(effective, "after");
                if (N(row, "DoiTuong_ID") != op.HistoryId || before == null || after == null ||
                    !AuditEquals(ExecutionEditAudit.Object(prior, "before"), op.Before) ||
                    !OperationAfterEquals(op.After, ExecutionEditAudit.Object(prior, "after")) || !ValidCutCorrection(op, before, after))
                    throw new ExecutionEditException(op.Token, "Nhật ký điều chỉnh chiều dài cắt không khớp sổ thao tác.");
                op.Before = before;
                op.After = after;
            }
        }

        private static bool AuditEquals(object? left, object? right)
        {
            if (left is Dictionary<string, object> a && right is Dictionary<string, object> b)
                return a.Count == b.Count && a.All(x => b.TryGetValue(x.Key, out var value) && AuditEquals(x.Value, value));
            return Equals(left, right);
        }

        private static bool SameExcept(Dictionary<string, object> original, Dictionary<string, object> proposed, params string[] changed) =>
            original.Keys.All(proposed.ContainsKey) && original.Keys.Except(changed).All(x => AuditEquals(original[x], proposed[x]));

        private static bool OperationAfterEquals(Dictionary<string, object> original, Dictionary<string, object>? proposed)
        {
            if (proposed == null || !SameExcept(original, proposed, "history")) return false;
            var a = ExecutionEditAudit.Object(original, "history");
            var b = ExecutionEditAudit.Object(proposed, "history");
            return a != null && b != null && SameExcept(a, b, "NguoiThucHien");
        }

        private static bool ValidCutCorrection(ExecutionOperation op, Dictionary<string, object> before, Dictionary<string, object> after)
        {
            var priorHistory = ExecutionEditAudit.Object(op.After, "history");
            var history = ExecutionEditAudit.Object(after, "history");
            var previous = ExecutionEditAudit.Object(before, "reel");
            var previousOriginal = ExecutionEditAudit.Object(op.Before, "reel");
            var remaining = ExecutionEditAudit.Object(after, "reel");
            var remainingOriginal = ExecutionEditAudit.Object(op.After, "reel");
            if (!SameExcept(op.Before, before, "reel") || !SameExcept(op.After, after, "reel", "history") ||
                priorHistory == null || history == null || remaining == null || remainingOriginal == null ||
                !SameExcept(priorHistory, history, CorrectedCutFields.Append("NguoiThucHien").ToArray()) ||
                !SameExcept(remainingOriginal, remaining, "SoDau", "SoCuoi", "TrangThai") ||
                (previous == null) != (previousOriginal == null)) return false;

            var baseMarkers = CutMarkers.Read(history, "Truoc");
            int length = checked((int)ExecutionEditAudit.Number(history, "ChieuDaiCat"));
            if (!baseMarkers.TryCut(length, out var expected) ||
                ExecutionEditAudit.Number(history, "HeSoChieu") != baseMarkers.Direction ||
                ExecutionEditAudit.Number(history, "SoDauCat") != baseMarkers.End ||
                ExecutionEditAudit.Number(history, "SoCuoiCat") != expected.End) return false;
            var reelMarkers = CutMarkers.Read(remaining);
            if (!reelMarkers.Equals(CutMarkers.Read(history, "Sau")) ||
                (!reelMarkers.Equals(expected) && !reelMarkers.Equals(expected.Swap())) ||
                !Equals(remaining["TrangThai"], expected.Length == 0 ? "HET" : "ACTIVE")) return false;
            if (previous == null) return true;
            var physical = CutMarkers.Read(previous);
            return SameExcept(previousOriginal!, previous, "SoDau", "SoCuoi", "TrangThai") &&
                physical.Start >= 0 && physical.End >= 0 && physical.Length == baseMarkers.Length &&
                Equals(previous["TrangThai"], physical.Length == 0 ? "HET" : "ACTIVE") &&
                history.TryGetValue("DieuChinhThongSo", out var numbering) && Equals(numbering, DescribeNumberingChange(physical, baseMarkers));
        }

        internal static CutMarkers ReplayMarkerEvents(ExecutionUndoContext context, ExecutionOperation operation, CutMarkers current)
        {
            foreach (var marker in context.MarkerEvents.Where(x => x.BasisToken == operation.Token).OrderBy(x => x.Id))
                current = CutMarkers.Replay(CutMarkers.Read(ExecutionEditAudit.Parse(marker.BeforeJson)),
                    CutMarkers.Read(ExecutionEditAudit.Parse(marker.AfterJson)), current, operation.Token);
            return current;
        }

        internal static CutLengthPlan PlanCutLengths(ExecutionUndoContext context, IReadOnlyCollection<CutLengthEdit> edits,
            bool checkPlan, string? stopBeforeToken = null)
        {
            var plan = new CutLengthPlan();
            if (edits.Count == 0) return plan;
            var byToken = edits.ToDictionary(x => x.OperationToken);
            var visibleTokens = new HashSet<string>(context.Snapshot.Rows.Select(x => x.OperationToken));
            var selected = context.Operations.Where(x => byToken.ContainsKey(x.Token) && x.Kind == "CUT" && x.ReelId.HasValue).ToList();
            if (selected.Count != edits.Count)
                throw new ExecutionEditException("cut", "Không tìm thấy lần cắt đang sửa.");

            foreach (var reel in selected.GroupBy(x => x.ReelId!.Value))
            {
                long reelId = reel.Key;
                var chain = context.Operations.Where(x => x.Kind == "CUT" && x.ReelId == reelId).OrderBy(x => x.Sequence).ToList();
                long first = reel.Min(x => x.Sequence);
                long last = reel.Max(x => x.Sequence);
                if (context.LiveCuts.Values.Any(x => ExecutionEditAudit.Number(x, "TonCuonLe_ID") == reelId &&
                    !chain.Any(o => o.HistoryId == ExecutionEditAudit.Number(x, "id"))))
                    throw new ExecutionEditException("REEL:" + reelId, "Cuộn có lần cắt thiếu sổ thao tác để đối chiếu thứ tự.");
                CutMarkers previous = default, oldPrevious = default;
                bool replaying = false;
                string note = "";
                foreach (var op in chain)
                {
                    var original = ExecutionEditAudit.Object(op.After, "history");
                    if (original == null || !context.LiveCuts.TryGetValue(op.HistoryId, out var live) ||
                        original.Keys.Any(x => x != "NguoiThucHien" && !ExecutionUndoPlanner.Same(original, live, x)))
                        throw new ExecutionEditException(op.Token, "Lịch sử cắt không khớp sổ thao tác và dữ liệu điều chỉnh.");

                    int actual = checked((int)ExecutionEditAudit.Number(live, "ChieuDaiCat"));
                    int planned = context.PlannedCutLengths[op.HistoryId];
                    byToken.TryGetValue(op.Token, out var edit);
                    if (checkPlan && actual != planned && edit == null)
                    {
                        if (op.Sequence < last)
                            throw new ExecutionEditException(op.Token, "Hãy sửa lần cắt sai chiều dài trước đó của cuộn trước.");
                        if (visibleTokens.Contains(op.Token))
                            throw new ExecutionEditException(op.Token, "Đoạn #" + ExecutionEditAudit.Number(live, "KeHoachCatChiTiet_ID") +
                                " phải khớp chiều dài kế hoạch trước khi lưu chuỗi cắt.");
                    }
                    if (edit != null && ((checkPlan && edit.Length != planned) || (edit.Direction != 1 && edit.Direction != -1)))
                        throw new ExecutionEditException(op.Token, "Chiều dài cắt phải khớp chiều dài kế hoạch và có chiều đánh số hợp lệ.");
                    if (op.Sequence < first) continue;

                    var oldBeforeState = ExecutionEditAudit.Object(op.Before, "reel");
                    var oldAfterState = ExecutionEditAudit.Object(op.After, "reel") ??
                        throw new ExecutionEditException(op.Token, "Thiếu mốc cuộn sau lần cắt.");
                    var originalBeforeCut = CutMarkers.Read(live, "Truoc");
                    CutMarkers beforeCut = originalBeforeCut;
                    var before = new Dictionary<string, object>(op.Before);
                    if (replaying)
                    {
                        if (oldBeforeState == null) throw new ExecutionEditException(op.Token, "Thiếu mốc cuộn trước lần cắt.");
                        var oldBefore = CutMarkers.Read(oldBeforeState);
                        var current = CutMarkers.Replay(oldPrevious, oldBefore, previous, op.Token);
                        before["reel"] = ReelWithMarkers(oldBeforeState, current);
                        beforeCut = ReplayCutNumbering(oldBefore, originalBeforeCut, current, op.Token);
                    }
                    if (op.Token == stopBeforeToken)
                    {
                        plan.NextBefore = beforeCut;
                        return plan;
                    }
                    if (edit != null)
                    {
                        beforeCut = beforeCut.InDirection(edit.Direction);
                        note = edit.Note;
                    }
                    int length = edit?.Length ?? actual;
                    if (!beforeCut.TryCut(length, out var afterCut))
                        throw new ExecutionEditException(op.Token, "Chiều dài cắt vượt quá chiều dài cuộn trước lần cắt.");
                    if (!originalBeforeCut.TryCut(actual, out var originalAfterCut))
                        throw new ExecutionEditException(op.Token, "Mốc cắt gốc không khớp chiều dài đã ghi nhận.");
                    var oldAfter = CutMarkers.Read(oldAfterState);
                    var remaining = edit != null ? (edit.ReverseAfter ? afterCut.Swap() : afterCut) :
                        CutMarkers.Replay(originalAfterCut, oldAfter, afterCut, op.Token);
                    var history = new Dictionary<string, object>(live)
                    {
                        ["ChieuDaiCat"] = length, ["SoDauTruoc"] = beforeCut.Start, ["SoCuoiTruoc"] = beforeCut.End,
                        ["SoDauCat"] = beforeCut.End, ["SoCuoiCat"] = afterCut.End,
                        ["SoDauSau"] = remaining.Start, ["SoCuoiSau"] = remaining.End, ["HeSoChieu"] = beforeCut.Direction
                    };
                    var physicalBefore = ExecutionEditAudit.Object(before, "reel");
                    if (physicalBefore != null)
                        history["DieuChinhThongSo"] = DescribeNumberingChange(CutMarkers.Read(physicalBefore), beforeCut)!;
                    var after = new Dictionary<string, object>(op.After)
                    {
                        ["reel"] = ReelWithMarkers(oldAfterState, remaining), ["history"] = history
                    };
                    plan.Cuts.Add(new CorrectedCut { Operation = op, Before = before, After = after, History = history,
                        BeforeCut = beforeCut, Remaining = remaining, Note = note });
                    oldPrevious = ReplayMarkerEvents(context, op, oldAfter);
                    previous = ReplayMarkerEvents(context, op, remaining);
                    replaying = true;
                }
                var tail = chain.Last();
                if (!context.CurrentReels.TryGetValue(reelId, out var stock) ||
                    !CutMarkers.Read(stock).Equals(oldPrevious))
                    throw new ExecutionEditException(tail.Token, "Mốc tồn hiện tại không khớp với chuỗi cắt.");
                plan.Remaining.Add(reelId, previous);
            }
            return plan;
        }

        private static CutMarkers ReplayCutNumbering(CutMarkers original, CutMarkers adjusted, CutMarkers current, string key)
        {
            // A recorded pre-cut direction is a choice of end, even if an earlier correction already reversed the reel.
            if (!original.Equals(adjusted) && original.Swap().Equals(adjusted))
                return current.InDirection(adjusted.Direction);
            if (original.Direction == adjusted.Direction && original.End != adjusted.End && original.Length == adjusted.Length)
                return current.MoveToEnd(adjusted.End, current.Direction, key);
            return CutMarkers.Replay(original, adjusted, current, key);
        }

        private static string? DescribeNumberingChange(CutMarkers before, CutMarkers after)
        {
            if (before.Equals(after)) return null;
            var changed = new List<string>();
            if (before.End != after.End) changed.Add("so_cuoi");
            if (before.Direction != after.Direction) changed.Add("chieu");
            return ExecutionEditAudit.Json(new { phien_ban = 1,
                truoc_dieu_chinh = new { so_dau = before.Start, so_cuoi = before.End, chieu = before.Direction },
                sau_dieu_chinh = new { so_dau = after.Start, so_cuoi = after.End, chieu = after.Direction },
                truong_nguoi_dung_thay_doi = changed, so_dau_tinh_lai = before.Start != after.Start });
        }

        internal static Dictionary<string, object> ReelWithMarkers(Dictionary<string, object> reel, CutMarkers markers) =>
            new Dictionary<string, object>(reel) { ["SoDau"] = markers.Start, ["SoCuoi"] = markers.End,
                ["TrangThai"] = markers.Length == 0 ? "HET" : "ACTIVE" };

        private static void ApplyCutLengths(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest request, CutLengthPlan plan)
        {
            foreach (var cut in plan.Cuts)
            {
                var op = cut.Operation;
                var priorHistory = Table(conn, tx, "SELECT * FROM LichSuCat WHERE id=@id", "@id", op.HistoryId).Rows[0];
                var priorAfter = new Dictionary<string, object>(op.After) { ["history"] = ExecutionEditAudit.Row(priorHistory) };
                var parameters = CorrectedCutFields.SelectMany(key => new object[] { "@" + key, cut.History[key] }).
                    Concat(new object[] { "@id", op.HistoryId }).ToArray();
                var assignments = string.Join(",", CorrectedCutFields.Select(key => key + "=@" + key));
                if (Exec(conn, tx, "UPDATE LichSuCat SET " + assignments + " WHERE id=@id", parameters) != 1)
                    throw new ExecutionEditException(op.Token, "Lần cắt đã thay đổi.", true);
                var savedHistory = Table(conn, tx, "SELECT * FROM LichSuCat WHERE id=@id", "@id", op.HistoryId).Rows[0];
                cut.After["history"] = ExecutionEditAudit.Row(savedHistory);
                Audit(conn, tx, request, "DOI_MOC", "CUT", op.HistoryId, op.Token, null, null, CutLengthReason, cut.Note,
                    new { before = op.Before, after = priorAfter }, new { before = cut.Before, after = cut.After });
            }
            foreach (var pair in plan.Remaining)
            {
                var markers = pair.Value;
                if (Exec(conn, tx, "UPDATE TonCuonLe SET SoDau=@start,SoCuoi=@end,TrangThai=@status WHERE id=@id",
                    "@start", markers.Start, "@end", markers.End, "@status", markers.Length == 0 ? "HET" : "ACTIVE", "@id", pair.Key) != 1)
                    throw new ExecutionEditException("REEL:" + pair.Key, "Cuộn đã thay đổi.", true);
            }
        }
    }
}
