using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    internal sealed class ExecutionEditDraft
    {
        public ExecutionDetailSnapshot Baseline { get; }
        private readonly Dictionary<string, ExecutionRowEdit> _rows = new Dictionary<string, ExecutionRowEdit>();
        private readonly Dictionary<long, ReelMarkerEdit> _reels = new Dictionary<long, ReelMarkerEdit>();
        public IReadOnlyDictionary<string, ExecutionRowEdit> RowEdits => _rows;
        public IReadOnlyDictionary<long, ReelMarkerEdit> ReelEdits => _reels;
        public bool HasChanges => _rows.Count != 0 || _reels.Count != 0;
        public ExecutionEditDraft(ExecutionDetailSnapshot baseline) { Baseline = baseline; }
        public void SetRowEdit(ExecutionRowEdit edit)
        {
            var row = Baseline.Rows.SingleOrDefault(x => x.Done && x.OperationToken.Length != 0 && x.OperationToken == edit.OperationToken);
            if (row == null) throw new InvalidOperationException("Không thể tạo lần thực hiện mới tại B2.");
            if (!edit.Undo && edit.NewPerformerUsername == row.PerformerUsername) _rows.Remove(edit.OperationToken);
            else _rows[edit.OperationToken] = Copy(edit);
        }
        public void SetReelEdit(ReelMarkerEdit edit)
        {
            if (!Baseline.Reels.TryGetValue(edit.ReelId, out var reel)) throw new InvalidOperationException("Cuộn không thuộc phạm vi đang xem.");
            if (edit.NewEnd == reel.End && edit.NewDirection == reel.Direction) _reels.Remove(edit.ReelId);
            else _reels[edit.ReelId] = Copy(edit);
        }
        public ExecutionEditRequest BuildRequest(Guid changeId) => new ExecutionEditRequest {
            ChangeId = changeId, Query = Baseline.Query, ExpectedVersions = new Dictionary<string, long>(Baseline.Versions),
            RowEdits = _rows.Values.Select(Copy).ToList(), ReelEdits = _reels.Values.Select(Copy).ToList() };
        public void Reset() { _rows.Clear(); _reels.Clear(); }
        private static ExecutionRowEdit Copy(ExecutionRowEdit e) => new ExecutionRowEdit {
            OperationToken = e.OperationToken, NewPerformerUsername = e.NewPerformerUsername, Undo = e.Undo, Reason = e.Reason, Note = e.Note };
        private static ReelMarkerEdit Copy(ReelMarkerEdit e) => new ReelMarkerEdit {
            ReelId = e.ReelId, NewEnd = e.NewEnd, NewDirection = e.NewDirection, Reason = e.Reason, Note = e.Note };
    }
}
