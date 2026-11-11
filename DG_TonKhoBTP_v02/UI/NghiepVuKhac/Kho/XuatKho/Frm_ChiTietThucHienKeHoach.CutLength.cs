using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    public partial class Frm_ChiTietThucHienKeHoach
    {
        private const string CutImpactMessage = "Các thay đổi này sẽ khiến mốc đầu/cuối các đoạn khác có thể thay đổi. Kiểm tra kỹ trên dây trước khi lưu.";

        private sealed class CutInput
        {
            internal string Text = "";
            internal int Direction;
            internal bool ReverseAfter;
        }

        private readonly Dictionary<string, CutInput> _cutInputs = new Dictionary<string, CutInput>();
        private readonly Dictionary<string, int> _cutDirections = new Dictionary<string, int>();
        private int _cutBaseDirection;
        private bool HasCutImpact => _markerInputs.Count != 0 || ChangedCuts().Any() ||
            _snapshot.Rows.Any(row => _cutDirections.ContainsKey(row.OperationToken));

        private void SetCutLengthReadOnly(bool readOnly, bool reversed = false)
        {
            textBox2.ReadOnly = readOnly;
            _cutLengthTip.SetToolTip(textBox2, reversed ? "Đổi về chiều vừa tính để sửa lại chiều dài cắt." : "");
        }

        private bool IsCutEditor(ExecutionDetailRow? row) => row?.ActualCutLength.HasValue == true &&
            row.ReelId.HasValue && row.CutSequence > 0 && _snapshot.CutContext != null;

        private static bool ParseLength(string text, out int length) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out length) && length > 0;

        private bool CutChanged(ExecutionDetailRow row, CutInput input) =>
            !ParseLength(input.Text, out int length) || length != row.ActualCutLength ||
            input.Direction != row.CutDirection || input.ReverseAfter != SavedCutWasReversed(row);

        private static bool SavedCutWasReversed(ExecutionDetailRow row) => row.ActualCutLength.HasValue &&
            new CutMarkers(row.BeforeStart, row.BeforeEnd).TryCut(row.ActualCutLength.Value, out var after) &&
            after.Length > 0 && after.Swap().Equals(new CutMarkers(row.AfterStart, row.AfterEnd));

        private IEnumerable<ExecutionDetailRow> ChangedCuts() => _snapshot.Rows.Where(row =>
            _cutInputs.TryGetValue(row.OperationToken, out var input) && CutChanged(row, input));

        private CutLengthEdit CutEdit(ExecutionDetailRow row)
        {
            var input = _cutInputs[row.OperationToken];
            ParseLength(input.Text, out int length);
            return new CutLengthEdit { OperationToken = row.OperationToken, Length = length,
                Direction = input.Direction, ReverseAfter = input.ReverseAfter, Note = rtbLyDoSua.Text };
        }

        private bool EarlierCutNeedsCorrection(ExecutionDetailRow row)
        {
            var context = _snapshot.CutContext!;
            return context.Operations.Where(x => x.Kind == "CUT" && x.ReelId == row.ReelId && x.Sequence < row.CutSequence)
                .Any(x => context.LiveCuts.TryGetValue(x.HistoryId, out var history) &&
                    ExecutionEditAudit.Number(history, "ChieuDaiCat") != context.PlannedCutLengths[x.HistoryId] &&
                    (!_cutInputs.TryGetValue(x.Token, out var input) || !ParseLength(input.Text, out int length) ||
                    length != context.PlannedCutLengths[x.HistoryId]));
        }

        private CutMarkers? MarkersBeforeCut(ExecutionDetailRow row)
        {
            var earlier = ChangedCuts().Where(x => x.ReelId == row.ReelId && x.CutSequence < row.CutSequence)
                .Select(CutEdit).ToList();
            if (earlier.Count == 0) return new CutMarkers(row.BeforeStart, row.BeforeEnd);
            try
            {
                return ExecutionEdit_DB.PlanCutLengths(_snapshot.CutContext!, earlier, false, row.OperationToken).NextBefore;
            }
            catch (ExecutionEditException) { return null; }
        }

        private void BindCutEditor(ExecutionDetailRow row, bool undoing)
        {
            _cutInputs.TryGetValue(row.OperationToken, out var input);
            var before = MarkersBeforeCut(row);
            _cutBaseDirection = before?.Direction ?? row.CutDirection;
            int direction = input?.Direction ?? (_cutDirections.TryGetValue(row.OperationToken, out int chosen) ? chosen :
                _cutBaseDirection);
            var marks = before?.InDirection(direction);
            bool validLength = input == null;
            if (marks.HasValue && input != null && ParseLength(input.Text, out int length) && marks.Value.TryCut(length, out var after))
            {
                marks = input.ReverseAfter ? after.Swap() : after;
                validLength = true;
            }
            tbxSoDau_Sua.Text = marks?.Start.ToString(CultureInfo.InvariantCulture) ?? "";
            tbxSoCuoi_Sua.Text = validLength ? marks?.End.ToString(CultureInfo.InvariantCulture) ?? "" : "";
            cbxChieu.SelectedValue = input?.ReverseAfter == true ? -direction : direction;
            textBox2.Text = input?.Text ?? row.ActualCutLength?.ToString(CultureInfo.InvariantCulture) ?? "—";

            bool editable = !_saving && _snapshot.EditingEnabled && !undoing && before.HasValue && before.Value.Length > 0 &&
                !EarlierCutNeedsCorrection(row);
            tbxSoCuoi_Sua.Enabled = true;
            cbxChieu.Enabled = editable && validLength;
            bool reversed = input?.ReverseAfter == true;
            SetCutLengthReadOnly(!editable || reversed, reversed);
        }

        private void CutLengthChanged()
        {
            var row = SelectedRow();
            if (_binding || _saving || textBox2.ReadOnly || !IsCutEditor(row)) return;
            if (!_cutInputs.TryGetValue(row!.OperationToken, out var input))
            {
                input = new CutInput { Direction = cbxChieu.SelectedValue is int d ? d : row.CutDirection };
                _cutInputs.Add(row.OperationToken, input);
                _cutDirections.Remove(row.OperationToken);
            }
            input.Text = textBox2.Text;
            BindSelected();
            Changed();
        }

        private void DirectionChanged()
        {
            if (_binding || _saving || !cbxChieu.Enabled || !(cbxChieu.SelectedValue is int direction) ||
                !(SelectedRow()?.ReelId is long reelId) || !_snapshot.Reels.TryGetValue(reelId, out var reel)) return;
            var row = SelectedRow()!;
            string start = tbxSoDau_Sua.Text;
            tbxSoDau_Sua.Text = tbxSoCuoi_Sua.Text;
            tbxSoCuoi_Sua.Text = start;
            if (IsCutEditor(row))
            {
                if (_cutInputs.TryGetValue(row.OperationToken, out var input))
                {
                    input.ReverseAfter = direction != input.Direction;
                    SetCutLengthReadOnly(input.ReverseAfter, input.ReverseAfter);
                }
                else if (direction == _cutBaseDirection) _cutDirections.Remove(row.OperationToken);
                else _cutDirections[row.OperationToken] = direction;
                Changed();
                return;
            }

            int end = int.Parse(tbxSoCuoi_Sua.Text, CultureInfo.InvariantCulture);
            _draft.SetReelEdit(new ReelMarkerEdit { ReelId = reelId, NewEnd = end, NewDirection = direction, Reason = "MARKERS" });
            if (end == reel.End && direction == reel.Direction) _markerInputs.Remove(reelId);
            else _markerInputs[reelId] = new MarkerInput { End = tbxSoCuoi_Sua.Text, Direction = direction };
            Changed();
        }
    }
}
