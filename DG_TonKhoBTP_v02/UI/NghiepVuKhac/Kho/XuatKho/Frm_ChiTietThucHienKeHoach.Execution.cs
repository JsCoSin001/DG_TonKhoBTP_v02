using DG_TonKhoBTP_v02.Database.Kho.XuatKho;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.XuatKho
{
    public partial class Frm_ChiTietThucHienKeHoach
    {
        private sealed class MarkerInput
        {
            internal string End = "";
            internal int Direction;
        }
        private ExecutionDetailSnapshot _snapshot = null!;
        private ExecutionEditDraft _draft = null!;
        private readonly string _productName = "";
        private readonly Dictionary<long, MarkerInput> _markerInputs = new Dictionary<long, MarkerInput>();
        private Timer _previewTimer = null!;
        private Guid _changeId = Guid.NewGuid();
        private int _generation;
        private string _batchImpact = "";
        private bool _binding, _saving, _closing, _allowClose;
        internal ExecutionEditResult? LastSaveResult { get; private set; }
        internal Func<string, MessageBoxButtons, DialogResult>? DialogPrompt { get; set; }
        private bool HasChanges => _draft.HasChanges || _markerInputs.Count != 0;

        internal Frm_ChiTietThucHienKeHoach(ExecutionDetailQuery query, string productName)
            : this(query, productName, ExecutionEdit_DB.Load(query)) { }

        internal Frm_ChiTietThucHienKeHoach(ExecutionDetailQuery query, string productName, ExecutionDetailSnapshot snapshot)
            : this()
        {
            _snapshot = snapshot; _draft = new ExecutionEditDraft(snapshot);
            _productName = productName?.Trim() ?? "";
            BindingContext = new BindingContext(); StartPosition = FormStartPosition.CenterParent;
            components ??= new Container();
            _previewTimer = new Timer(components) { Interval = 350 };
            ConfigureDataControls();
            grvDsChiTietCuon.CurrentCellChanged += (s, e) => { if (!_binding) BindSelected(); };
            grvDsChiTietCuon.CurrentCellDirtyStateChanged += (s, e) => {
                if (grvDsChiTietCuon.IsCurrentCellDirty) grvDsChiTietCuon.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grvDsChiTietCuon.CellValueChanged += StatusChanged;
            grvDsChiTietCuon.DataError += (s, e) => { e.ThrowException = false; ShowError("Giá trị ô không hợp lệ."); };
            cbxNguoiThucHien.SelectedValueChanged += (s, e) => PersonChanged();
            tbxSoCuoi_Sua.TextChanged += (s, e) => MarkersChanged();
            cbxChieu.SelectedValueChanged += (s, e) => MarkersChanged();
            rtbLyDoSua.TextChanged += (s, e) => { if (!_binding && !_saving) Changed(); };
            _previewTimer.Tick += async (s, e) => { _previewTimer.Stop(); await PreviewAsync(); };
            btnLuuThayDoi.Click += async (s, e) => await SaveAsync();
            btnĐóng.Click += (s, e) => Close(); FormClosing += ClosingWithDraft;
            Disposed += (s, e) => _generation++;
            tabControl1.SelectedIndexChanged += (s, e) => { if (tabControl1.SelectedTab == tabPage2) LoadAudit(); };
            BindAll(); ShowError("");
        }

        private void ConfigureDataControls()
        {
            var grid = grvDsChiTietCuon;
            grid.AutoGenerateColumns = false; grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
            grid.MultiSelect = false; grid.RowHeadersVisible = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.Columns.Clear();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoLuong", HeaderText = "Số lượng", Width = 65, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colChieuDai", HeaderText = "Chiều dài (m)", Width = 85, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNghiepVu", HeaderText = "Nghiệp vụ", Width = 125, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNhom", HeaderText = "Nhóm / đoạn", Width = 85, ReadOnly = true });
            grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "colTrangThai", HeaderText = "Trạng thái", Width = 135,
                DataSource = new[] { "Đã thực hiện", "Chưa thực hiện" }, FlatStyle = FlatStyle.Flat });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaCuon", HeaderText = "QR / Mã cuộn", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            // Single-line text cells use native end ellipsis without shortening their stored values.
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.AllowUserToResizeRows = false;
            grid.RowTemplate.MinimumHeight = 32;
            grid.RowTemplate.Height = 32;
            grid.ShowCellToolTips = true;
            cbxNguoiThucHien.DropDownStyle = cbxChieu.DropDownStyle = ComboBoxStyle.DropDownList;
            dtNgayThucHien.Enabled = false;
            tbxQr.ReadOnly = tbxNguonNhap.ReadOnly = tbxSoDau_Sua.ReadOnly = true;
            tbxSoDau_SoCuoi_DB.ReadOnly = tbxSoDau_SoCuoi_DaCat_DB.ReadOnly = tbxSoDau_SoCuoi_ConLai_DB.ReadOnly = true;
            dataGridView2.ReadOnly = true; dataGridView2.AllowUserToAddRows = dataGridView2.AllowUserToDeleteRows = false;
            dataGridView2.AutoGenerateColumns = true; dataGridView2.RowHeadersVisible = false;
            // Preserve the designed error row; standard Label ellipsis exposes long text on hover.
            lblTacDong.AutoSize = false; lblTacDong.Dock = DockStyle.Fill; lblTacDong.AutoEllipsis = true;
        }

        private ExecutionDetailRow? SelectedRow() => grvDsChiTietCuon.CurrentRow?.Tag as ExecutionDetailRow;
        private void BindAll()
        {
            string key = SelectedRow()?.RowKey ?? ""; _binding = true;
            try
            {
                cbxNguoiThucHien.DataSource = _snapshot.Users.ToList(); cbxNguoiThucHien.DisplayMember = "DisplayName"; cbxNguoiThucHien.ValueMember = "Username";
                cbxChieu.DataSource = new Dictionary<int, string> { [1] = "Tăng (+1)", [-1] = "Giảm (-1)" }.ToList();
                cbxChieu.DisplayMember = "Value"; cbxChieu.ValueMember = "Key";
                grvDsChiTietCuon.Rows.Clear();
                foreach (var row in _snapshot.Rows)
                {
                    _draft.RowEdits.TryGetValue(row.OperationToken, out var edit);
                    int i = grvDsChiTietCuon.Rows.Add(row.Quantity, row.Length, row.Description,
                        row.GroupId.HasValue ? row.GroupId + " / " + row.DetailId : "—", edit?.Undo == true ? "Chưa thực hiện" : row.Status, row.RawQr);
                    var r = grvDsChiTietCuon.Rows[i]; r.Tag = row;
                    r.Cells["colTrangThai"].ReadOnly = !_snapshot.EditingEnabled || !row.Done || row.OperationToken.Length == 0;
                    if (row.RowKey == key) grvDsChiTietCuon.CurrentCell = r.Cells[0];
                }
                if (grvDsChiTietCuon.CurrentCell == null && grvDsChiTietCuon.Rows.Count > 0) grvDsChiTietCuon.CurrentCell = grvDsChiTietCuon.Rows[0].Cells[0];
            }
            finally { _binding = false; }
            BindHeader(); BindSelected(); UpdateDraftState();
        }

        private void BindHeader()
        {
            lblTenSP.Text = _productName.Length > 0 ? _productName : "—";
            var details = new List<string>();
            if (_snapshot.Query.PartialReelId is long reelId)
            {
                details.Add("Cuộn lẻ");
                if (_snapshot.Reels.TryGetValue(reelId, out var reel))
                {
                    details.Add(reel.Code);
                    details.Add("Tồn hiện tại: " + reel.Length + " m");
                }
            }
            else
            {
                details.Add("Cuộn chẵn");
                var takes = _snapshot.Rows.Where(x => x.Kind == "TAKE").ToList();
                if (takes.Count > 0)
                {
                    details.Add(takes[0].Length + " m/cuộn");
                    details.Add("Lấy nguyên: " + takes.Where(x => x.Done).Sum(x => (long)x.Quantity) + "/" + takes.Sum(x => (long)x.Quantity) + " cuộn");
                }
            }
            var cuts = _snapshot.Rows.Where(x => x.Kind == "CUT").ToList();
            if (cuts.Count > 0) details.Add("Cắt: " + cuts.Count(x => x.Done) + "/" + cuts.Count + " đoạn");
            lblThongTinSP.Text = string.Join(" · ", details);
        }

        private void BindSelected()
        {
            var row = SelectedRow(); _binding = true;
            try
            {
                bool editable = !_saving && _snapshot.EditingEnabled && row?.Done == true && !string.IsNullOrEmpty(row.OperationToken);
                ExecutionRowEdit? edit = null;
                if (row != null) _draft.RowEdits.TryGetValue(row.OperationToken, out edit);
                cbxNguoiThucHien.SelectedValue = edit?.NewPerformerUsername ?? row?.PerformerUsername ?? "";
                cbxNguoiThucHien.Enabled = editable && edit?.Undo != true;
                dtNgayThucHien.Format = DateTimePickerFormat.Custom; dtNgayThucHien.CustomFormat = "'—'";
                if (row?.Done == true && DateTime.TryParse(row.PerformedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var performed))
                { dtNgayThucHien.Value = performed; dtNgayThucHien.CustomFormat = "dd/MM/yyyy HH:mm:ss"; }
                tbxQr.Text = row?.RawQr ?? "";
                tbxNguonNhap.Text = row?.ReelId is long rid && _snapshot.Reels.TryGetValue(rid, out var sourceReel)
                    ? sourceReel.SourceLabel : !string.IsNullOrEmpty(row?.SourceLabel) ? row!.SourceLabel : "—";
                bool cut = row?.Kind == "CUT" && row.Done;
                tbxSoDau_SoCuoi_DB.Text = cut ? row!.BeforeStart + " → " + row.BeforeEnd : "—";
                tbxSoDau_SoCuoi_DaCat_DB.Text = cut ? row!.CutStart + " → " + row.CutEnd : "—";
                tbxSoDau_SoCuoi_ConLai_DB.Text = cut ? row!.AfterStart + " → " + row.AfterEnd : "—";
                bool hasReel = row?.ReelId is long id && _snapshot.Reels.ContainsKey(id);
                tbxSoDau_Sua.Visible = tbxSoCuoi_Sua.Visible = cbxChieu.Visible = hasReel;
                tbxSoCuoi_Sua.Enabled = cbxChieu.Enabled = false;
                if (hasReel)
                {
                    var reel = _snapshot.Reels[row!.ReelId!.Value]; _markerInputs.TryGetValue(reel.ReelId, out var input);
                    tbxSoCuoi_Sua.Text = input?.End ?? reel.End.ToString(CultureInfo.InvariantCulture); cbxChieu.SelectedValue = input?.Direction ?? reel.Direction;
                    tbxSoDau_Sua.Text = TryMarker(reel, tbxSoCuoi_Sua.Text, input?.Direction ?? reel.Direction, out _, out long start) ? start.ToString() : "";
                    bool undoing = _draft.RowEdits.Values.Any(x => x.Undo && _snapshot.Rows.Any(r => r.OperationToken == x.OperationToken && r.ReelId == reel.ReelId));
                    tbxSoCuoi_Sua.Enabled = cbxChieu.Enabled = !_saving && _snapshot.EditingEnabled && reel.Status == "ACTIVE" && reel.Length > 0 && !undoing;
                }
                else { tbxSoDau_Sua.Text = tbxSoCuoi_Sua.Text = ""; }
                rtbLyDoSua.Enabled = !_saving && _snapshot.EditingEnabled;
            }
            finally { _binding = false; }
            if (tabControl1.SelectedTab == tabPage2) LoadAudit();
        }

        private static bool TryMarker(ExecutionReelState reel, string text, int direction, out int end, out long start)
        {
            start = -1;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out end) || end < 0 || (direction != 1 && direction != -1)) return false;
            start = (long)end - direction * reel.Length; return start >= 0 && start <= int.MaxValue;
        }
        private void StatusChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_binding || _saving || e.RowIndex < 0 || e.ColumnIndex != grvDsChiTietCuon.Columns["colTrangThai"].Index) return;
            var row = (ExecutionDetailRow)grvDsChiTietCuon.Rows[e.RowIndex].Tag;
            if (!row.Done || !_snapshot.EditingEnabled || row.OperationToken.Length == 0) { BindAll(); return; }
            _draft.RowEdits.TryGetValue(row.OperationToken, out var old);
            bool undo = Convert.ToString(grvDsChiTietCuon.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) == "Chưa thực hiện";
            _draft.SetRowEdit(new ExecutionRowEdit { OperationToken = row.OperationToken, NewPerformerUsername = old?.NewPerformerUsername ?? row.PerformerUsername,
                Undo = undo, Reason = undo ? "MISTAKE" : "INFO" });
            BindSelected(); Changed();
        }
        private void PersonChanged()
        {
            if (_binding || _saving) return;
            var row = SelectedRow(); if (row == null || !row.Done || !_snapshot.EditingEnabled || row.OperationToken.Length == 0) return;
            _draft.RowEdits.TryGetValue(row.OperationToken, out var old);
            _draft.SetRowEdit(new ExecutionRowEdit { OperationToken = row.OperationToken, NewPerformerUsername = Convert.ToString(cbxNguoiThucHien.SelectedValue) ?? row.PerformerUsername,
                Undo = old?.Undo == true, Reason = old?.Undo == true ? "MISTAKE" : "INFO" });
            Changed();
        }
        private void MarkersChanged()
        {
            if (_binding || _saving || !(SelectedRow()?.ReelId is long id) || !_snapshot.Reels.TryGetValue(id, out var reel)) return;
            int direction = cbxChieu.SelectedValue is int d ? d : reel.Direction;
            var input = new MarkerInput { End = tbxSoCuoi_Sua.Text, Direction = direction }; _markerInputs[id] = input;
            bool valid = TryMarker(reel, input.End, direction, out int end, out long start); tbxSoDau_Sua.Text = valid ? start.ToString() : "";
            if (valid)
            {
                _draft.SetReelEdit(new ReelMarkerEdit { ReelId = id, NewEnd = end, NewDirection = direction, Reason = "MARKERS" });
                if (end == reel.End && direction == reel.Direction) _markerInputs.Remove(id);
            }
            Changed();
        }
        private ExecutionEditRequest BuildRequest()
        {
            var request = _draft.BuildRequest(_changeId);
            foreach (var edit in request.RowEdits) { edit.Reason = edit.Undo ? "MISTAKE" : "INFO"; edit.Note = rtbLyDoSua.Text; }
            foreach (var edit in request.ReelEdits) { edit.Reason = "MARKERS"; edit.Note = rtbLyDoSua.Text; }
            return request;
        }
        private string InputError()
        {
            foreach (var pair in _markerInputs)
                if (!_snapshot.Reels.TryGetValue(pair.Key, out var reel) || !TryMarker(reel, pair.Value.End, pair.Value.Direction, out _, out _))
                    return "Mốc cuộn " + (_snapshot.Reels.TryGetValue(pair.Key, out var r) ? r.Code : pair.Key.ToString()) +
                        " không hợp lệ.";
            return string.IsNullOrWhiteSpace(rtbLyDoSua.Text) ? "Vui lòng nhập lý do sửa trước khi lưu." : "";
        }
        private void UpdateDraftState()
        {
            btnLuuThayDoi.Enabled = !_saving && _snapshot.EditingEnabled && HasChanges;
            foreach (DataGridViewRow r in grvDsChiTietCuon.Rows)
                r.DefaultCellStyle.BackColor = _draft.RowEdits.ContainsKey(((ExecutionDetailRow)r.Tag).OperationToken) ? Color.LightGoldenrodYellow : grvDsChiTietCuon.DefaultCellStyle.BackColor;
        }
        private void Changed()
        {
            _generation++; _batchImpact = ""; ShowError(""); UpdateDraftState(); _previewTimer.Stop();
            if (HasChanges && InputError().Length == 0) _previewTimer.Start();
        }
        private async Task PreviewAsync()
        {
            if (_saving || !HasChanges || IsDisposed || InputError().Length != 0) return;
            int generation = _generation; var request = BuildRequest();
            try
            {
                var preview = await Task.Run(() => ExecutionEdit_DB.Preview(request));
                if (!IsDisposed && !_saving && generation == _generation) _batchImpact = preview.Valid ? ConfirmationText(request, preview) : preview.Message;
            }
            catch (Exception) { /* Background checks never interrupt typing. Save checks again and reports failures. */ }
        }
        private string ConfirmationText(ExecutionEditRequest request, ExecutionEditPreview preview)
        {
            var lines = new List<string>();
            if (request.RowEdits.Any(x => x.Undo)) lines.Add("Chỉ hoàn tác khi ghi nhận nhầm và thực tế chưa lấy / chưa cắt.");
            foreach (var edit in request.RowEdits)
            {
                var row = _snapshot.Rows.Single(x => x.OperationToken == edit.OperationToken);
                lines.Add((edit.Undo ? "Hoàn tác" : "Sửa người thực hiện") + ": " + row.Description + " · " + row.Quantity + " × " + row.Length + " m · " + row.RawQr);
            }
            foreach (var edit in request.ReelEdits)
            {
                var reel = _snapshot.Reels[edit.ReelId];
                lines.Add("Cuộn " + reel.Code + ": mốc hiện tại " + reel.Start + " → " + reel.End + "; mốc mới " +
                    ((long)edit.NewEnd - edit.NewDirection * reel.Length) + " → " + edit.NewEnd + "; giữ nguyên tồn " + reel.Length + " m.");
            }
            lines.AddRange(preview.ImpactByReelId.Values);
            // TAKE quantities come from the actual row above, not an assumed single reel.
            lines.AddRange(preview.ImpactByRowKey.Where(p => !_snapshot.Rows.Any(r => r.RowKey == p.Key && r.Kind == "TAKE")).Select(p => p.Value));
            lines.Add("Lý do sửa: " + rtbLyDoSua.Text); return string.Join(Environment.NewLine, lines);
        }
        private DialogResult Ask(string text, MessageBoxButtons buttons) => DialogPrompt != null ? DialogPrompt(text, buttons) :
            MessageBox.Show(this, text, Text, buttons, MessageBoxIcon.Question);
        private void ShowError(string message) { if (!IsDisposed) lblTacDong.Text = message; }

        private async Task<bool> SaveAsync()
        {
            if (_saving) return false;
            grvDsChiTietCuon.EndEdit(); if (!HasChanges) return true;
            string error = InputError(); if (error.Length > 0) { ShowError(error); return false; }
            var request = BuildRequest(); int generation = _generation;
            bool conflict = false, success = false, closeAfterSave = false;
            _saving = true; _previewTimer.Stop(); grvDsChiTietCuon.Enabled = false; BindSelected(); UpdateDraftState(); ShowError("");
            try
            {
                var preview = await Task.Run(() => ExecutionEdit_DB.Preview(request));
                if (IsDisposed) return false;
                if (!preview.Valid) { ShowError(preview.Message); conflict = preview.ErrorsByKey.ContainsKey("version"); }
                else
                {
                    _batchImpact = ConfirmationText(request, preview);
                    bool confirm = request.ReelEdits.Count > 0 || request.RowEdits.Any(x => x.Undo);
                    if (!confirm || Ask("Bạn thực sự muốn lưu?",MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (generation != _generation) ShowError("Nội dung đã thay đổi. Vui lòng kiểm tra và lưu lại.");
                        else
                        {
                            var result = await Task.Run(() => ExecutionEdit_DB.Save(request)); if (IsDisposed) return false;
                            if (result.Success)
                            {
                                LastSaveResult = result; success = true; _draft.Reset(); _markerInputs.Clear(); rtbLyDoSua.Clear(); _generation++; _changeId = Guid.NewGuid();
                                if (result.NewSnapshot != null && result.NewSnapshot.Rows.Count == 0)
                                {
                                    FrmWaiting.ShowGifAlert("Không có dữ liệu");
                                    closeAfterSave = true;
                                }
                                else if (result.NewSnapshot != null)
                                {
                                    _snapshot = result.NewSnapshot; _draft = new ExecutionEditDraft(_snapshot); BindAll();
                                }
                                else closeAfterSave = true;
                            }
                            else { ShowError(result.Message); conflict = result.Conflict; }
                        }
                    }
                }
            }
            catch (Exception) { ShowError("Lưu thất bại."); }
            finally
            {
                _saving = false;
                if (!IsDisposed) { grvDsChiTietCuon.Enabled = true; BindSelected(); UpdateDraftState(); }
            }
            if (!IsDisposed && conflict && Ask("Dữ liệu đã thay đổi. Tải dữ liệu mới để đối chiếu? Bản nháp của bạn vẫn được giữ; việc tải mới không tự lưu.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                ReloadPreservingDraft();
            if (!IsDisposed && closeAfterSave) { _allowClose = true; Close(); }
            return success;
        }
        private void ReloadPreservingDraft()
        {
            if (_saving) return;
            try
            {
                var latest = ExecutionEdit_DB.Load(_snapshot.Query);
                if (_draft.RowEdits.Keys.Any(t => !latest.Rows.Any(r => r.Done && r.OperationToken == t)) || _markerInputs.Keys.Any(id => !latest.Reels.ContainsKey(id)))
                { ShowError("Dòng hoặc cuộn đang sửa không còn hiệu lực. Bản nháp vẫn được giữ; đóng không lưu nếu muốn bỏ thay đổi."); return; }
                if (latest.Rows.Count == 0)
                {
                    if (HasChanges)
                    {
                        ShowError("Không có dữ liệu trong phạm vi này. Bản nháp vẫn được giữ; đóng không lưu nếu muốn bỏ thay đổi.");
                        return;
                    }
                    FrmWaiting.ShowGifAlert("Không có dữ liệu");
                    _allowClose = true;
                    Close();
                    return;
                }
                var draft = new ExecutionEditDraft(latest);
                var unchangedInputs = new List<long>();
                foreach (var edit in _draft.RowEdits.Values) draft.SetRowEdit(edit);
                foreach (var pair in _markerInputs)
                    if (TryMarker(latest.Reels[pair.Key], pair.Value.End, pair.Value.Direction, out int end, out _))
                    {
                        draft.SetReelEdit(new ReelMarkerEdit { ReelId = pair.Key, NewEnd = end, NewDirection = pair.Value.Direction, Reason = "MARKERS" });
                        if (end == latest.Reels[pair.Key].End && pair.Value.Direction == latest.Reels[pair.Key].Direction)
                            unchangedInputs.Add(pair.Key);
                    }
                _snapshot = latest; _draft = draft; _changeId = Guid.NewGuid();
                foreach (long id in unchangedInputs) _markerInputs.Remove(id);
                BindAll(); Changed();
            }
            catch (Exception) { ShowError("Không thể tải dữ liệu mới. Bản nháp vẫn được giữ."); }
        }
        private void LoadAudit()
        {
            try
            {
                var row = SelectedRow(); dataGridView2.DataSource = ExecutionEdit_DB.LoadAudit(_snapshot.Query.PlanId, row?.ReelId, row?.OperationToken ?? "");
                var headers = new Dictionary<string, string> { ["Loai"] = "Loại thay đổi", ["LyDo"] = "Loại lý do", ["GhiChu"] = "Lý do chi tiết",
                    ["NguoiSua"] = "Người sửa", ["NguoiSua_ID"] = "ID người sửa", ["ThoiGianSua"] = "Thời gian sửa",
                    ["TruocJson"] = "Dữ liệu trước", ["SauJson"] = "Dữ liệu sau", ["DoiTuong"] = "Đối tượng", ["DoiTuong_ID"] = "ID đối tượng",
                    ["MaLanLuu"] = "Mã lần lưu", ["MaThaoTac"] = "Mã thao tác", ["MaThaoTacCoSo"] = "Thao tác cơ sở", ["SuKienMoc_ID"] = "Sự kiện mốc" };
                foreach (var pair in headers) if (dataGridView2.Columns.Contains(pair.Key)) dataGridView2.Columns[pair.Key].HeaderText = pair.Value;
            }
            catch (Exception) { ShowError("Không thể tải lịch sử điều chỉnh."); }
        }
        private async void ClosingWithDraft(object sender, FormClosingEventArgs e)
        {
            if (_allowClose) return;
            if (_saving || _closing) { e.Cancel = true; return; }
            if (!HasChanges) return;
            e.Cancel = true;
            var choice = Ask("Có thay đổi chưa lưu. Lưu trước khi đóng? Chọn No để bỏ thay đổi và đóng; Cancel để tiếp tục sửa.", MessageBoxButtons.YesNoCancel);
            if (choice == DialogResult.No) { e.Cancel = false; return; }
            if (choice != DialogResult.Yes) return;
            _closing = true;
            try { if (await SaveAsync() && !IsDisposed) { _allowClose = true; Close(); } }
            finally { _closing = false; }
        }
    }
}
