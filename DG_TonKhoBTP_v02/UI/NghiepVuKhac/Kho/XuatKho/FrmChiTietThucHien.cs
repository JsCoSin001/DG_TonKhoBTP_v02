using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    internal sealed class FrmChiTietThucHien : Form
    {
        private readonly DataGridView _grid = new DataGridView { Name = "grvChiTietThucHien", Dock = DockStyle.Fill,
            AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            MultiSelect = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        private readonly ComboBox _person = new ComboBox { Name = "cbxNguoiThucHien", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly TextBox _time = new TextBox { Name = "txtThoiGianThucHien", ReadOnly = true, Dock = DockStyle.Fill };
        private readonly TextBox _qr = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
        private readonly TextBox _source = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
        private readonly TextBox _history = new TextBox { ReadOnly = true, Multiline = true, Height = 47, Dock = DockStyle.Fill };
        private readonly ComboBox _reason = new ComboBox { Name = "cbxLyDoSuaDong", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly NumericUpDown _end = new NumericUpDown { Name = "numSoCuoiHienTai", Maximum = int.MaxValue, Minimum = 0, Dock = DockStyle.Fill };
        private readonly TextBox _start = new TextBox { Name = "txtSoDauHienTai", ReadOnly = true, Dock = DockStyle.Fill };
        private readonly ComboBox _direction = new ComboBox { Name = "cbxChieuMoc", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly ComboBox _markerReason = new ComboBox { Name = "cbxLyDoSuaMoc", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly Label _reelTitle = new Label { AutoSize = true, Dock = DockStyle.Top };
        private readonly Label _impact = new Label { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(6), MaximumSize = new Size(0, 0) };
        private readonly Label _count = new Label { AutoSize = true, Margin = new Padding(6, 10, 6, 0) };
        private readonly Button _save = new Button { Name = "btnLuuThayDoi", Text = "Lưu thay đổi", AutoSize = true, Height = 34 };
        private readonly Button _discard = new Button { Text = "Bỏ thay đổi", AutoSize = true, Height = 34 };
        private readonly Button _close = new Button { Text = "Đóng", AutoSize = true, Height = 34 };
        private readonly GroupBox _markers = new GroupBox { Name = "grpMocCuonHienTai", Text = "Mốc cuộn hiện tại — độc lập với dòng lịch sử", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        private readonly DataGridView _audit = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = true, RowHeadersVisible = false };
        private readonly Timer _previewTimer = new Timer { Interval = 350 };
        private ExecutionDetailSnapshot _snapshot;
        private ExecutionEditDraft _draft;
        private Guid _changeId = Guid.NewGuid();
        private int _generation;
        private int _validatedGeneration = -1;
        private string _batchImpact = "";
        private readonly Button _reload = new Button { Name = "btnDoiChieuDuLieuMoi", Text = "Tải mới để đối chiếu", AutoSize = true, Visible = false };
        private bool _binding, _saving;
        public ExecutionEditResult? LastSaveResult { get; private set; }

        public FrmChiTietThucHien(ExecutionDetailQuery query, string productName)
            : this(query, productName, ExecutionEdit_DB.Load(query)) { }
        public FrmChiTietThucHien(ExecutionDetailQuery query, string productName, ExecutionDetailSnapshot snapshot)
        {
            _snapshot = snapshot; _draft = new ExecutionEditDraft(snapshot);
            Text = "Chi tiết thực hiện kế hoạch"; StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1220, 900); MinimumSize = new Size(950, 700); Padding = new Padding(8);
            Font = new Font("Tahoma", 10F); BindingContext = new BindingContext();
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 49));
            var title = new Label { Dock = DockStyle.Fill, Text = "Chi tiết thực hiện: " + productName + Environment.NewLine +
                "Sửa trực tiếp; lịch sử cắt và thời gian thực hiện chỉ xem.", Padding = new Padding(4) };
            layout.Controls.Add(title, 0, 0); layout.Controls.Add(_grid, 0, 1);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var info = new TabPage("Thông tin thực hiện") { AutoScroll = true, Padding = new Padding(8) };
            var audit = new TabPage("Lịch sử điều chỉnh"); audit.Controls.Add(_audit);
            tabs.TabPages.Add(info); tabs.TabPages.Add(audit); layout.Controls.Add(tabs, 0, 2);
            var detail = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 6 };
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            detail.Controls.Add(Field("Người thực hiện · có thể chọn tài khoản ngừng hoạt động", _person), 0, 0);
            detail.Controls.Add(Field("Thời gian thực hiện · chỉ xem", _time), 1, 0);
            detail.Controls.Add(Field("QR / Mã cuộn · chỉ xem", _qr), 0, 1);
            detail.Controls.Add(Field("Nguồn cuộn · chỉ xem", _source), 1, 1);
            var history = Field("Mốc của lần cắt đang chọn · lịch sử chỉ xem", _history);
            detail.Controls.Add(history, 0, 2); detail.SetColumnSpan(history, 2);
            var reason = Field("Lý do sửa dòng · bắt buộc khi dòng có thay đổi", _reason);
            detail.Controls.Add(reason, 0, 3); detail.SetColumnSpan(reason, 2);
            var markerLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 3 };
            for (int i = 0; i < 3; i++) markerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));
            markerLayout.Controls.Add(_reelTitle, 0, 0); markerLayout.SetColumnSpan(_reelTitle, 3);
            markerLayout.Controls.Add(Field("Số đầu · tự tính", _start), 0, 1);
            markerLayout.Controls.Add(Field("Số cuối · có thể sửa", _end), 1, 1);
            markerLayout.Controls.Add(Field("Chiều mốc · có thể sửa", _direction), 2, 1);
            var markerReason = Field("Lý do sửa mốc cuộn · bắt buộc khi mốc có thay đổi", _markerReason);
            markerLayout.Controls.Add(markerReason, 0, 2); markerLayout.SetColumnSpan(markerReason, 3); _markers.Controls.Add(markerLayout);
            detail.Controls.Add(_markers, 0, 4); detail.SetColumnSpan(_markers, 2);
            detail.Controls.Add(_impact, 0, 5); detail.SetColumnSpan(_impact, 2); info.Controls.Add(detail);
            detail.SizeChanged += (s, e) => _impact.MaximumSize = new Size(Math.Max(200, detail.ClientSize.Width - 16), 0);
            detail.SizeChanged += (s, e) => _impact.MaximumSize = new Size(Math.Max(200, detail.ClientSize.Width - 20), 0);
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(3) };
            bottom.Controls.AddRange(new Control[] { _close, _save, _discard, _reload, _count }); layout.Controls.Add(bottom, 0, 3);
            Controls.Add(layout);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoLuong", HeaderText = "Số lượng", Width = 95, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colChieuDai", HeaderText = "Chiều dài (m)", Width = 110, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNghiepVu", HeaderText = "Nghiệp vụ", Width = 170, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNhom", HeaderText = "Nhóm / đoạn", Width = 115, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "colTrangThai", HeaderText = "Trạng thái · sửa trực tiếp", Width = 200,
                DataSource = new[] { "Đã thực hiện", "Chưa thực hiện" }, FlatStyle = FlatStyle.Flat });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaCuon", HeaderText = "QR / Mã cuộn", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 260, ReadOnly = true });
            _grid.RowTemplate.MinimumHeight = 35; _grid.DefaultCellStyle.Padding = new Padding(4);
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing; _grid.ColumnHeadersHeight = 44;
            _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True; _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            // SelectionChanged can fire before CurrentRow moves to the clicked row.
            _grid.CurrentCellChanged += (s, e) => { if (!_binding) BindSelected(); };
            _grid.CurrentCellDirtyStateChanged += (s, e) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            _grid.CellValueChanged += StatusChanged;
            _grid.DataError += (s, e) => { e.ThrowException = false; _impact.Text = "Giá trị ô không hợp lệ."; };
            _person.SelectedValueChanged += (s, e) => RowChanged(false);
            _reason.SelectedValueChanged += (s, e) => RowChanged(true);
            _end.ValueChanged += (s, e) => MarkersChanged(); _direction.SelectedValueChanged += (s, e) => MarkersChanged();
            _markerReason.SelectedValueChanged += (s, e) => MarkersChanged();
            _previewTimer.Tick += async (s, e) => { _previewTimer.Stop(); await PreviewAsync(); };
            _save.Click += async (s, e) => await SaveAsync();
            _reload.Click += (s, e) => ReloadPreservingDraft();
            _discard.Click += (s, e) => { _draft.Reset(); _generation++; _changeId = Guid.NewGuid(); BindAll(); };
            _close.Click += (s, e) => Close(); FormClosing += ClosingWithDraft;
            tabs.SelectedIndexChanged += (s, e) => {
                if (tabs.SelectedTab == audit) {
                    try { var row = SelectedRow(); _audit.DataSource = ExecutionEdit_DB.LoadAudit(_snapshot.Query.PlanId, row?.ReelId, row?.OperationToken ?? ""); }
                    catch (Exception) { _impact.Text = "Không thể tải lịch sử điều chỉnh."; }
                }
            };
            BindAll();
        }
        private static Control Field(string label, Control control)
        {
            var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, Margin = new Padding(4) };
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 2, 0, 4) }, 0, 0);
            panel.Controls.Add(control, 0, 1); return panel;
        }
        private static void Reasons(ComboBox combo, Dictionary<string, string> choices)
        { combo.DataSource = choices.ToList(); combo.DisplayMember = "Value"; combo.ValueMember = "Key"; }
        private ExecutionDetailRow? SelectedRow() => _grid.CurrentRow?.Tag as ExecutionDetailRow;
        private void BindAll()
        {
            string key = SelectedRow()?.RowKey ?? ""; _binding = true;
            _person.DataSource = _snapshot.Users.ToList(); _person.DisplayMember = "DisplayName"; _person.ValueMember = "Username";
            Reasons(_reason, new Dictionary<string, string> { [""] = "Chọn lý do…", ["INFO"] = "Sai người thực hiện", ["MISTAKE"] = "Ghi nhận nhầm — thực tế chưa lấy / chưa cắt" });
            Reasons(_markerReason, new Dictionary<string, string> { [""] = "Chọn lý do…", ["MARKERS"] = "Sai mốc số — giữ nguyên chiều dài tồn" });
            _direction.DataSource = new Dictionary<int, string> { [1] = "Tăng (+1)", [-1] = "Giảm (−1)" }.ToList();
            _direction.DisplayMember = "Value"; _direction.ValueMember = "Key";
            _grid.Rows.Clear();
            foreach (var row in _snapshot.Rows)
            {
                _draft.RowEdits.TryGetValue(row.OperationToken, out var edit);
                int i = _grid.Rows.Add(row.Quantity, row.Length, row.Description,
                    row.GroupId.HasValue ? row.GroupId + " / " + row.DetailId : "—", edit?.Undo == true ? "Chưa thực hiện" : row.Status, row.RawQr);
                var gridRow = _grid.Rows[i]; gridRow.Tag = row;
                gridRow.Cells["colTrangThai"].ReadOnly = !_snapshot.EditingEnabled || !row.Done || row.OperationToken.Length == 0;
                if (edit != null) gridRow.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                if (row.RowKey == key) _grid.CurrentCell = gridRow.Cells[0];
            }
            if (_grid.CurrentCell == null && _grid.Rows.Count > 0) _grid.CurrentCell = _grid.Rows[0].Cells[0];
            _binding = false; BindSelected(); UpdateDraftState();
        }
        private void BindSelected()
        {
            var row = SelectedRow(); _binding = true;
            try
            {
                bool editable = !_saving && _snapshot.EditingEnabled && row?.Done == true && !string.IsNullOrEmpty(row.OperationToken);
                ExecutionRowEdit? edit = null;
                if (row != null) _draft.RowEdits.TryGetValue(row.OperationToken, out edit);
                _person.SelectedValue = edit?.NewPerformerUsername ?? row?.PerformerUsername ?? "";
                _person.Enabled = editable && edit?.Undo != true;
                _time.Text = row?.PerformedAt ?? ""; _qr.Text = row?.RawQr ?? "";
                _source.Text = row?.ReelId is long rid && _snapshot.Reels.ContainsKey(rid) ? _snapshot.Reels[rid].SourceLabel :
                    !string.IsNullOrEmpty(row?.SourceLabel) ? row!.SourceLabel : "—";
                _history.Text = (row?.Kind == "CUT" && row.Done ? "Trước cắt: " + row.BeforeStart + " → " + row.BeforeEnd +
                    "     |     Đoạn cắt: " + row.CutStart + " → " + row.CutEnd + "     |     Sau cắt: " + row.AfterStart + " → " + row.AfterEnd : "—") +
                    (row?.Done == true ? "     |     Người đang lưu: " + row.PerformerUsername : "");
                _reason.SelectedValue = edit?.Reason ?? ""; _reason.Enabled = editable && edit != null;
                _markers.Visible = row?.ReelId != null;
                if (row?.ReelId is long reelId && _snapshot.Reels.TryGetValue(reelId, out var reel))
                {
                    _draft.ReelEdits.TryGetValue(reelId, out var marker);
                    _reelTitle.Text = "Cuộn " + reel.Code + " · Tồn hiện tại " + reel.Length + " m · Mốc đang lưu " + reel.Start + " → " + reel.End;
                    _end.Value = marker?.NewEnd ?? reel.End; _direction.SelectedValue = marker?.NewDirection ?? reel.Direction;
                    long start = (long)_end.Value - (marker?.NewDirection ?? reel.Direction) * reel.Length;
                    _start.Text = start.ToString(); _markerReason.SelectedValue = marker?.Reason ?? "";
                    bool undoing = _draft.RowEdits.Values.Any(x => x.Undo && _snapshot.Rows.Any(r => r.OperationToken == x.OperationToken && r.ReelId == reelId));
                    _end.Enabled = _direction.Enabled = !_saving && _snapshot.EditingEnabled && reel.Length > 0 && reel.Status == "ACTIVE" && !undoing;
                    _markerReason.Enabled = _end.Enabled && marker != null;
                }
                _impact.Text = _draft.HasChanges && _batchImpact.Length > 0 ? _batchImpact :
                    !_snapshot.EditingEnabled ? (_snapshot.PlanStatus == "HUY" ? "Kế hoạch đã hủy — chỉ xem." : "Chức năng sửa chưa bật sau cập nhật B2/B3 — chỉ xem.") :
                    "Chọn trạng thái ngay trên bảng hoặc sửa người / mốc cuộn tại đây.";
            }
            finally { _binding = false; }
        }
        private void StatusChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_binding || e.RowIndex < 0 || e.ColumnIndex != _grid.Columns["colTrangThai"].Index) return;
            var row = (ExecutionDetailRow)_grid.Rows[e.RowIndex].Tag;
            if (!row.Done || !_snapshot.EditingEnabled) { BindAll(); return; }
            _draft.RowEdits.TryGetValue(row.OperationToken, out var old);
            bool undo = Convert.ToString(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) == "Chưa thực hiện";
            _draft.SetRowEdit(new ExecutionRowEdit { OperationToken = row.OperationToken, NewPerformerUsername = old?.NewPerformerUsername ?? row.PerformerUsername,
                Undo = undo, Reason = undo ? (old?.Reason == "MISTAKE" ? old.Reason : _draft.RowEdits.Values.FirstOrDefault(x => x.Undo && x.Reason == "MISTAKE")?.Reason ?? "") :
                    (old?.Undo == true ? "" : old?.Reason ?? "") });
            BindSelected(); Changed();
        }
        private void RowChanged(bool reasonChanged)
        {
            if (_binding || _saving) return;
            var row = SelectedRow(); if (row == null || !row.Done || !_snapshot.EditingEnabled) return;
            _draft.RowEdits.TryGetValue(row.OperationToken, out var old);
            var edit = new ExecutionRowEdit { OperationToken = row.OperationToken, NewPerformerUsername = Convert.ToString(_person.SelectedValue) ?? row.PerformerUsername,
                Undo = old?.Undo ?? false, Reason = Convert.ToString(_reason.SelectedValue) ?? "" };
            _draft.SetRowEdit(edit);
            if (reasonChanged && edit.Undo && edit.Reason == "MISTAKE")
                foreach (var other in _draft.RowEdits.Values.Where(x => x.Undo).ToList())
                    _draft.SetRowEdit(new ExecutionRowEdit { OperationToken = other.OperationToken, NewPerformerUsername = other.NewPerformerUsername,
                        Undo = true, Reason = "MISTAKE", Note = other.Note });
            _reason.Enabled = _draft.RowEdits.ContainsKey(row.OperationToken); Changed();
        }
        private void MarkersChanged()
        {
            if (_binding || _saving || !(SelectedRow()?.ReelId is long id) || !_snapshot.Reels.TryGetValue(id, out var reel)) return;
            int direction = _direction.SelectedValue is int d ? d : reel.Direction;
            long start = (long)_end.Value - direction * reel.Length; _start.Text = start.ToString();
            _draft.SetReelEdit(new ReelMarkerEdit { ReelId = id, NewEnd = (int)_end.Value, NewDirection = direction,
                Reason = Convert.ToString(_markerReason.SelectedValue) ?? "" });
            _markerReason.Enabled = _draft.ReelEdits.ContainsKey(id) && _end.Enabled; Changed();
        }
        private void UpdateDraftState()
        {
            _save.Enabled = !_saving && _snapshot.EditingEnabled && _draft.HasChanges && _validatedGeneration == _generation;
            _discard.Enabled = !_saving && _draft.HasChanges;
            _count.Text = _draft.HasChanges ? (_draft.RowEdits.Count + " dòng, " + _draft.ReelEdits.Count + " cuộn có thay đổi · Chưa lưu") : "Chưa có thay đổi";
            foreach (DataGridViewRow r in _grid.Rows)
                r.DefaultCellStyle.BackColor = _draft.RowEdits.ContainsKey(((ExecutionDetailRow)r.Tag).OperationToken) ? Color.LightGoldenrodYellow : _grid.DefaultCellStyle.BackColor;
        }
        private void Changed()
        {
            _generation++; _batchImpact = _draft.HasChanges ? "Đang kiểm tra tác động của toàn bộ thay đổi…" : "";
            if (_draft.HasChanges) _impact.Text = _batchImpact;
            UpdateDraftState(); _previewTimer.Stop(); if (_draft.HasChanges) _previewTimer.Start();
        }
        private async Task PreviewAsync()
        {
            if (_saving || !_draft.HasChanges || IsDisposed) return;
            int generation = _generation; var request = _draft.BuildRequest(_changeId);
            ExecutionEditPreview preview;
            try { preview = await Task.Run(() => ExecutionEdit_DB.Preview(request)); }
            catch (Exception) { if (!IsDisposed) _impact.Text = "Không thể kiểm tra dữ liệu. Bản nháp vẫn được giữ."; return; }
            if (IsDisposed || generation != _generation || _saving) return;
            _validatedGeneration = preview.Valid ? generation : -1;
            _reload.Visible = !preview.Valid && preview.Message.Contains("đã thay đổi");
            UpdateDraftState();
            _batchImpact = preview.Valid ? string.Join(Environment.NewLine, preview.ImpactByRowKey.Values.Concat(preview.ImpactByReelId.Values)) : preview.Message;
            if (preview.Valid && _batchImpact.Length == 0) _batchImpact = "Sửa người thực hiện; giữ nguyên tồn và thời gian. Tài khoản người sửa được lưu riêng.";
            _impact.Text = _batchImpact;
        }
        private async Task<bool> SaveAsync()
        {
            if (_saving || !_draft.HasChanges) return !_draft.HasChanges;
            if (_validatedGeneration != _generation) { await PreviewAsync(); return false; }
            _previewTimer.Stop(); _generation++; _saving = true; _grid.Enabled = false; BindSelected(); UpdateDraftState();
            _impact.Text = "Đang kiểm tra và lưu toàn bộ thay đổi…";
            ExecutionEditResult result;
            try { result = await Task.Run(() => ExecutionEdit_DB.Save(_draft.BuildRequest(_changeId))); }
            catch (Exception) { result = new ExecutionEditResult { ErrorsByKey = new Dictionary<string, string> { ["database"] = "Không thể lưu thay đổi. Bản nháp vẫn được giữ; vui lòng kiểm tra dữ liệu rồi thử lại." } }; }
            if (IsDisposed) return false;
            _saving = false; _grid.Enabled = true;
            if (result.Success)
            {
                LastSaveResult = result;
                if (result.NewSnapshot != null) { _snapshot = result.NewSnapshot; _draft = new ExecutionEditDraft(_snapshot); _changeId = Guid.NewGuid(); BindAll(); }
                else { _draft.Reset(); Close(); }
                _impact.Text = "Đã lưu thay đổi. Tài khoản người sửa: " + UserContext.UserName;
            }
            else { _validatedGeneration = -1; BindSelected(); Changed(); _reload.Visible = result.Conflict; _impact.Text = result.Message; }
            return result.Success;
        }
        private void ReloadPreservingDraft()
        {
            if (_saving) return;
            try
            {
                var latest = ExecutionEdit_DB.Load(_snapshot.Query);
                if (_draft.RowEdits.Keys.Any(t => !latest.Rows.Any(r => r.Done && r.OperationToken == t)) ||
                    _draft.ReelEdits.Keys.Any(id => !latest.Reels.ContainsKey(id)))
                { _impact.Text = "Dòng hoặc cuộn đang sửa không còn hiệu lực. Bản nháp vẫn được giữ; bỏ thay đổi này trước khi tải mới."; return; }
                var draft = new ExecutionEditDraft(latest);
                foreach (var edit in _draft.RowEdits.Values) draft.SetRowEdit(edit);
                foreach (var edit in _draft.ReelEdits.Values) draft.SetReelEdit(edit);
                _snapshot = latest; _draft = draft; _changeId = Guid.NewGuid(); _reload.Visible = false;
                BindAll(); Changed();
            }
            catch (Exception) { _impact.Text = "Không thể tải dữ liệu mới. Bản nháp vẫn được giữ."; }
        }
        private async void ClosingWithDraft(object sender, FormClosingEventArgs e)
        {
            if (_saving) { e.Cancel = true; return; }
            if (!_draft.HasChanges) return;
            var choice = MessageBox.Show(this, "Có thay đổi chưa lưu. Lưu trước khi đóng?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.No) return;
            e.Cancel = true;
            if (choice == DialogResult.Yes && await SaveAsync()) Close();
        }
        protected override void Dispose(bool disposing)
        { if (disposing) { _generation++; _previewTimer.Stop(); _previewTimer.Dispose(); } base.Dispose(disposing); }
    }
}
