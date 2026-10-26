using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal static class ExecutionUndoPlanner
    {
        private static long Number(Dictionary<string, object> row, string key) => ExecutionEditAudit.Number(row, key);
        private static bool Same(Dictionary<string, object> left, Dictionary<string, object> right, string key) =>
            left.TryGetValue(key, out object a) && right.TryGetValue(key, out object b) &&
            Convert.ToString(a, CultureInfo.InvariantCulture) == Convert.ToString(b, CultureInfo.InvariantCulture);
        private static void Require(bool condition, string key, string message)
        { if (!condition) throw new ExecutionEditException(key, message); }

        public static ExecutionUndoPlan Build(ExecutionEditRequest request, ExecutionUndoContext context)
        {
            var tokens = new HashSet<string>(request.RowEdits.Where(x => x.Undo).Select(x => x.OperationToken));
            var plan = new ExecutionUndoPlan { UndoOperations = context.Operations.Where(x => tokens.Contains(x.Token)).OrderByDescending(x => x.Sequence).ToList() };
            Require(plan.UndoOperations.Count == tokens.Count, "undo", "Không tìm thấy đầy đủ các thao tác còn hiệu lực.");
            foreach (var op in plan.UndoOperations)
            {
                var live = op.Kind == "CUT" ? context.LiveCuts : context.LiveTakes;
                var original = ExecutionEditAudit.Object(op.After, "history");
                Require(original != null && live.ContainsKey(op.HistoryId), op.Token, "Lịch sử thực hiện không còn khớp sổ thao tác.");
                foreach (var key in original!.Keys.Where(x => x != "NguoiThucHien"))
                    Require(Same(original, live[op.HistoryId], key), op.Token, "Lịch sử thực hiện đã thay đổi ngoài phạm vi được sửa.");
                if (op.Kind == "TAKE")
                {
                    Require(Number(live[op.HistoryId], "SoLuong") == 1 && context.QrUsages.Count(x => Number(x, "LichSuLayCuon_ID") == op.HistoryId) == 1 && context.QrUsages.Any(x =>
                        Number(x, "LichSuLayCuon_ID") == op.HistoryId && Same(x, new Dictionary<string, object> { ["RawQr"] = op.Qr }, "RawQr")),
                        op.Token, "Không xác định được QR duy nhất của lần lấy cuộn.");
                }
            }
            foreach (var group in plan.UndoOperations.Where(x => x.Kind == "CUT").GroupBy(x => x.ReelId!.Value))
            {
                long reelId = group.Key;
                var chain = context.Operations.Where(x => x.Kind == "CUT" && x.ReelId == reelId).OrderBy(x => x.Sequence).ToList();
                var first = group.OrderBy(x => x.Sequence).First();
                var dependent = chain.FirstOrDefault(x => x.Sequence >= first.Sequence && !tokens.Contains(x.Token));
                Require(dependent == null, first.Token, "Cuộn còn lần cắt phía sau chưa được chọn hoàn tác" +
                    (dependent == null ? "." : " (kế hoạch #" + dependent.PlanId + ", thao tác " + dependent.Token + ")."));
                Require(context.CurrentReels.ContainsKey(reelId), first.Token, "Cuộn thực tế đã thay đổi.");
                var current = context.CurrentReels[reelId];
                var last = chain.Last();
                var expected = ExecutionEditAudit.Object(last.After, "reel");
                var lastMarker = context.MarkerEvents.Where(x => x.ReelId == reelId && x.BasisToken == last.Token).OrderByDescending(x => x.Id).FirstOrDefault();
                if (lastMarker != null) expected = ExecutionEditAudit.Parse(lastMarker.AfterJson);
                Require(expected != null, first.Token, "Thiếu trạng thái cuộn sau thực hiện.");
                foreach (string key in new[] { "id", "SoDau", "SoCuoi", "TrangThai", "MaCuon", "TTCuonDay_ID", "TTThanhPham_ID" })
                    Require(Same(expected!, current, key), first.Token, "Trạng thái cuộn không khớp chuỗi thực hiện và điều chỉnh mốc.");
                // Every effective history on this physical reel needs its immutable operation record.
                Require(context.LiveCuts.Values.Where(x => Number(x, "TonCuonLe_ID") == reelId)
                    .All(x => chain.Any(o => o.HistoryId == Number(x, "id"))), first.Token, "Cuộn có lịch sử thiếu mã thao tác, không được suy đoán để hoàn tác.");
                var before = ExecutionEditAudit.Object(first.Before, "reel");
                var initialGroup = ExecutionEditAudit.Object(first.Before, "group");
                long groupId = initialGroup == null ? 0 : Number(initialGroup, "id");
                if (before == null)
                {
                    Require(first.Before.ContainsKey("reel") && ExecutionEditAudit.Number(first.Before, "version") == 1 &&
                        Convert.ToString(ExecutionEditAudit.Object(first.After, "history")!["LoaiNguon"]) == "CUON_CHAN",
                        first.Token, "Thiếu bản gốc hợp lệ trước khi chuyển cuộn chẵn thành lẻ.");
                    Require(initialGroup != null && Number(initialGroup, "TonCuonLe_ID") == 0 && groupId > 0,
                        first.Token, "Thiếu trạng thái nhóm trước chuyển cuộn chẵn thành lẻ.");
                    Require(context.Groups.Where(x => x.ReelId == reelId).All(x => x.GroupId == groupId),
                        first.Token, "Cuộn lẻ phát sinh còn được nhóm/kế hoạch khác sử dụng hoặc đặt trước.");
                    Require(context.QrUsages.Count(x => Number(x, "TonCuonLe_ID") == reelId) == 1 && context.QrUsages.Any(x => Number(x, "TonCuonLe_ID") == reelId &&
                        Convert.ToString(x["RawQr"]) == first.Qr), first.Token, "QR chuyển lẻ không còn khớp cuộn.");
                }
                int start = before == null ? 0 : checked((int)Number(before, "SoDau"));
                int end = before == null ? 0 : checked((int)Number(before, "SoCuoi"));
                Require(start >= 0 && end >= 0, first.Token, "Mốc gốc của cuộn không hợp lệ.");
                plan.ReelRestores.Add(new ExecutionReelRestore { ReelId = reelId, Remove = before == null, GroupId = groupId,
                    Start = start, End = end, Status = start == end ? "HET" : "ACTIVE", FirstToken = first.Token });
                plan.MarkerEventsToSupersede.AddRange(context.MarkerEvents.Where(x => x.ReelId == reelId && tokens.Contains(x.BasisToken)));
            }
            return plan;
        }
    }
}
