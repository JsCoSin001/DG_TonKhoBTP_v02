using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.Dictionary;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.Kho.NhapKho
{
    public partial class UC_NhapKhoNVL : UserControl
    {
        private const string COL_STT = "stt";
        private const string COL_TEN = "ten";
        private const string COL_KHOI_LUONG = "khoiLuong";
        private const string COL_CHIEU_DAI = "chieuDaiNhap";
        private const string COL_QR = "qr";
        private const string COL_GHI_CHU = "ghiChu";
        private const string COL_XOA = "colXoa";
        private const string FITST_CHARACTOR_QR = "X0";
        private const int GRID_ROW_HEIGHT = 30;

        private const string COL_CONG_DOAN = "congDoan";


        private readonly string[] _cotDuocPaste =
        {
            COL_CONG_DOAN,
            COL_TEN,
            COL_KHOI_LUONG,
            COL_QR,
            COL_GHI_CHU
        };

        private NhapKhoNVL_Dong _dongDangIn;

        public UC_NhapKhoNVL()
        {
            InitializeComponent();
            CaiDatGridVaSuKien();
            NapComboboxCongDoan();
        }

        private void NapComboboxCongDoan()
        {
            if (!dgvDsNhapNVL.Columns.Contains(COL_CONG_DOAN)) return;

            if (dgvDsNhapNVL.Columns[COL_CONG_DOAN] is DataGridViewComboBoxColumn colCongDoan)
            {
                var dsCongDoan = ThongTinChungCongDoan.TatCaCongDoan
                    .Where(x => x.Id != 2)
                    .OrderBy(x => x.Id)
                    .Select(x => new CongDoanComboItem
                    {
                        Id = x.Id,
                        TenCongDoan = CoreHelper.VietHoaKyTuDau(x.TenCongDoan)
                    })
                    .ToList();

                colCongDoan.DataSource = dsCongDoan;
                colCongDoan.ValueMember = nameof(CongDoanComboItem.Id);
                colCongDoan.DisplayMember = nameof(CongDoanComboItem.TenCongDoan);
                colCongDoan.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            }
        }

        private void KhoaNhapLieu(bool dangXuLy)
        {
            dgvDsNhapNVL.Enabled = !dangXuLy;
            btnLuu.Enabled = !dangXuLy;
            btnSua.Enabled = !dangXuLy;
            btnTaoQr.Enabled = !dangXuLy;
            tbxTimQr.Enabled = !dangXuLy;
        }

        private List<NhapKhoXuLyResult> LuuDanhSachTrongNen(List<NhapKhoXuLyItem> dsCanLuu)
        {
            List<NhapKhoXuLyResult> results = new List<NhapKhoXuLyResult>();

            foreach (NhapKhoXuLyItem item in dsCanLuu)
            {
                try
                {
                    NhapKhoNVL_Dong daLuu = NhapKhoNVL_DB.LuuMotDong(item.Input);

                    results.Add(new NhapKhoXuLyResult
                    {
                        Row = item.Row,
                        SttDong = item.SttDong,
                        Output = daLuu
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new NhapKhoXuLyResult
                    {
                        Row = item.Row,
                        SttDong = item.SttDong,
                        Error = ex
                    });
                }
            }

            return results;
        }

        private List<NhapKhoXuLyResult> SuaDanhSachTrongNen(List<NhapKhoXuLyItem> dsCanSua)
        {
            List<NhapKhoXuLyResult> results = new List<NhapKhoXuLyResult>();

            foreach (NhapKhoXuLyItem item in dsCanSua)
            {
                try
                {
                    NhapKhoNVL_Dong daSua = NhapKhoNVL_DB.CapNhatMotDong(item.Input);

                    results.Add(new NhapKhoXuLyResult
                    {
                        Row = item.Row,
                        SttDong = item.SttDong,
                        Output = daSua
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new NhapKhoXuLyResult
                    {
                        Row = item.Row,
                        SttDong = item.SttDong,
                        Error = ex
                    });
                }
            }

            return results;
        }

        private void CaiDatGridVaSuKien()
        {
            btnSua.Visible = false;
            btnLuu.Visible = true;

            btnSua.Click += btnSua_Click;

            dgvDsNhapNVL.AllowUserToAddRows = true;
            dgvDsNhapNVL.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvDsNhapNVL.MultiSelect = true;
            dgvDsNhapNVL.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dgvDsNhapNVL.AutoGenerateColumns = false;

            dgvDsNhapNVL.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvDsNhapNVL.RowTemplate.Height = GRID_ROW_HEIGHT;
            dgvDsNhapNVL.AllowUserToResizeRows = false;

            foreach (DataGridViewRow row in dgvDsNhapNVL.Rows)
            {
                row.Height = GRID_ROW_HEIGHT;
            }


            if (dgvDsNhapNVL.Columns.Contains(COL_STT))
                dgvDsNhapNVL.Columns[COL_STT].ReadOnly = true;

            if (dgvDsNhapNVL.Columns.Contains(COL_CHIEU_DAI))
                dgvDsNhapNVL.Columns[COL_CHIEU_DAI].Visible = false;

            if (dgvDsNhapNVL.Columns.Contains("ma"))
                dgvDsNhapNVL.Columns["ma"].Visible = false;

            if (dgvDsNhapNVL.Columns.Contains(COL_QR))
                dgvDsNhapNVL.Columns[COL_QR].Visible = true;

            if (dgvDsNhapNVL.Columns.Contains(COL_XOA) && dgvDsNhapNVL.Columns[COL_XOA] is DataGridViewButtonColumn colXoa)
            {
                colXoa.Text = "Xoá";
                colXoa.UseColumnTextForButtonValue = true;
            }

            dgvDsNhapNVL.CellClick += dgvDsNhapNVL_CellClick;
            dgvDsNhapNVL.KeyDown += dgvDsNhapNVL_KeyDown;
            dgvDsNhapNVL.CellContentClick += dgvDsNhapNVL_CellContentClick;
            dgvDsNhapNVL.CellValueChanged += dgvDsNhapNVL_CellValueChanged;
            dgvDsNhapNVL.RowsAdded += dgvDsNhapNVL_RowsAdded;
            dgvDsNhapNVL.RowsRemoved += dgvDsNhapNVL_RowsRemoved;
            dgvDsNhapNVL.UserDeletedRow += dgvDsNhapNVL_UserDeletedRow;
        }

        private void dgvDsNhapNVL_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            if (dgvDsNhapNVL.Columns[e.ColumnIndex].Name != COL_CONG_DOAN) return;

            dgvDsNhapNVL.BeginEdit(true);

            if (dgvDsNhapNVL.EditingControl is ComboBox comboBox)
            {
                comboBox.DroppedDown = true;
            }
        }

        private void tbxTimQr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            TimTheoQrVaHienThi();
        }

        private void TimTheoQrVaHienThi()
        {
            string maBin = tbxTimQr.Text.Trim();
            if (string.IsNullOrWhiteSpace(maBin))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập QR cần tìm.");
                return;
            }

            string[] arrSplitDash = maBin.Split('-');
            string[] arrSplitSemicolon = maBin.Split(';');

            if (arrSplitDash[0] != FITST_CHARACTOR_QR ||
                arrSplitSemicolon.Length != 27 ||
                arrSplitSemicolon[26].ToUpper() != "NOIBONK".ToUpper())
            {
                FrmWaiting.ShowGifAlert("Qr không hợp lệ");
                return;
            }


            try
            {
                NhapKhoNVL_Dong dong = NhapKhoNVL_DB.TimTheoMaBin(maBin);
                if (dong == null)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liệu theo QR đã nhập.");
                    return;
                }

                dgvDsNhapNVL.Rows.Clear();
                int rowIndex = dgvDsNhapNVL.Rows.Add();
                DataGridViewRow row = dgvDsNhapNVL.Rows[rowIndex];
                GanDongVaoGrid(row, dong);
                row.Tag = dong.TTThanhPhamId;

                btnSua.Visible = true;
                btnLuu.Visible = false;
                CapNhatSTT();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "TÌM QR"));
            }
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            dgvDsNhapNVL.EndEdit();

            List<DataGridViewRow> dsRow = LayDanhSachDongCoDuLieu();
            if (dsRow.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập dữ liệu cần lưu.");
                return;
            }

            List<NhapKhoXuLyItem> dsCanLuu = new List<NhapKhoXuLyItem>();
            int soDongLoi = 0;

            foreach (DataGridViewRow row in dsRow)
            {
                row.ErrorText = string.Empty;

                int sttDong = LaySTTHienThi(row);
                try
                {
                    NhapKhoNVL_Dong input = LayDuLieuTuGrid(row, batBuocCoId: false);

                    dsCanLuu.Add(new NhapKhoXuLyItem
                    {
                        Row = row,
                        SttDong = sttDong,
                        Input = input
                    });
                }
                catch (Exception ex)
                {
                    string loi = CoreHelper.ShowErrorDatabase(ex, $"DÒNG {sttDong}");
                    row.ErrorText = loi;
                    soDongLoi++;
                }
            }

            List<DataGridViewRow> dsRowLuuOk = new List<DataGridViewRow>();
            List<NhapKhoNVL_Dong> dsDaLuu = new List<NhapKhoNVL_Dong>();

            if (dsCanLuu.Count > 0)
            {
                FrmWaiting waiting = null;

                try
                {
                    KhoaNhapLieu(true);

                    waiting = new FrmWaiting("Đang lưu dữ liệu, vui lòng đợi...");
                    waiting.ControlBox = false;
                    waiting.ShowAndRefresh();

                    List<NhapKhoXuLyResult> results =
                        await Task.Run(() => LuuDanhSachTrongNen(dsCanLuu));

                    foreach (NhapKhoXuLyResult result in results)
                    {
                        if (result.ThanhCong)
                        {
                            dsDaLuu.Add(result.Output);
                            dsRowLuuOk.Add(result.Row);
                        }
                        else
                        {
                            string loi = CoreHelper.ShowErrorDatabase(result.Error, $"DÒNG {result.SttDong}");
                            result.Row.ErrorText = loi;
                            soDongLoi++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "LƯU DỮ LIỆU"));
                    return;
                }
                finally
                {
                    if (waiting != null)
                    {
                        waiting.CloseAndDispose();
                    }

                    KhoaNhapLieu(false);
                }
            }

            XoaCacDongDaXuLy(dsRowLuuOk);
            CapNhatSTT();

            if (soDongLoi > 0)
            {
                FrmWaiting.ShowGifAlert($"Có {soDongLoi} dòng chưa được lưu.");
            }

            if (dsDaLuu.Count > 0)
            {
                HoiVaInDanhSach(dsDaLuu);
            }
        }

        private async void btnSua_Click(object sender, EventArgs e)
        {
            dgvDsNhapNVL.EndEdit();

            List<DataGridViewRow> dsRow = LayDanhSachDongCoDuLieu();
            if (dsRow.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập hoặc tìm kiếm dữ liệu cần sửa.");
                return;
            }

            List<NhapKhoXuLyItem> dsCanSua = new List<NhapKhoXuLyItem>();
            int soDongLoi = 0;

            foreach (DataGridViewRow row in dsRow)
            {
                row.ErrorText = string.Empty;

                int sttDong = LaySTTHienThi(row);
                try
                {
                    NhapKhoNVL_Dong input = LayDuLieuTuGrid(row, batBuocCoId: true);

                    dsCanSua.Add(new NhapKhoXuLyItem
                    {
                        Row = row,
                        SttDong = sttDong,
                        Input = input
                    });
                }
                catch (Exception ex)
                {
                    string loi = CoreHelper.ShowErrorDatabase(ex, $"DÒNG {sttDong}");
                    row.ErrorText = loi;
                    soDongLoi++;
                }
            }

            List<NhapKhoNVL_Dong> dsDaSua = new List<NhapKhoNVL_Dong>();
            List<DataGridViewRow> dsRowSuaOk = new List<DataGridViewRow>();

            if (dsCanSua.Count > 0)
            {
                FrmWaiting waiting = null;

                try
                {
                    KhoaNhapLieu(true);

                    waiting = new FrmWaiting("Đang cập nhật dữ liệu, vui lòng đợi...");
                    waiting.ControlBox = false;
                    waiting.ShowAndRefresh();

                    List<NhapKhoXuLyResult> results =
                        await Task.Run(() => SuaDanhSachTrongNen(dsCanSua));

                    foreach (NhapKhoXuLyResult result in results)
                    {
                        if (result.ThanhCong)
                        {
                            dsDaSua.Add(result.Output);
                            dsRowSuaOk.Add(result.Row);
                        }
                        else
                        {
                            string loi = CoreHelper.ShowErrorDatabase(result.Error, $"DÒNG {result.SttDong}");
                            result.Row.ErrorText = loi;
                            soDongLoi++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, "CẬP NHẬT DỮ LIỆU"));
                    return;
                }
                finally
                {
                    if (waiting != null)
                    {
                        waiting.CloseAndDispose();
                    }

                    KhoaNhapLieu(false);
                }
            }

            XoaCacDongDaXuLy(dsRowSuaOk);
            CapNhatSTT();

            bool coDongSuaThanhCong = dsDaSua.Count > 0;

            if (coDongSuaThanhCong && soDongLoi == 0)
            {
                tbxTimQr.Clear();
                btnSua.Visible = false;
                btnLuu.Visible = true;
            }

            if (soDongLoi > 0)
                FrmWaiting.ShowGifAlert($"Có {soDongLoi} dòng chưa được sửa.");

            if (coDongSuaThanhCong)
            {
                FrmWaiting.ShowGifAlert($"Đã sửa thành công {dsDaSua.Count} dòng.", myIcon:EnumStore.Icon.Success);
                HoiVaInDanhSach(dsDaSua);
            }
        }

        private void dgvDsNhapNVL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.SuppressKeyPress = true;
                PasteVaoGrid();
            }
        }

        private void PasteVaoGrid()
        {
            if (!Clipboard.ContainsText()) return;

            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrEmpty(clipboardText)) return;

            int startRow = dgvDsNhapNVL.CurrentCell?.RowIndex ?? 0;
            int startPasteColIndex = LayViTriCotPaste(dgvDsNhapNVL.CurrentCell?.OwningColumn?.Name);

            string[] lines = clipboardText
                .TrimEnd('\r', '\n')
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            List<string> dsLoi = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                DataGridViewRow row = LayHoacTaoRow(startRow + i);
                row.ErrorText = string.Empty;

                string[] cells = lines[i].Split('\t');

                for (int j = 0; j < cells.Length; j++)
                {
                    int pasteColIndex = startPasteColIndex + j;
                    if (pasteColIndex >= _cotDuocPaste.Length)
                        break;

                    string colName = _cotDuocPaste[pasteColIndex];
                    if (!dgvDsNhapNVL.Columns.Contains(colName))
                        continue;

                    string cellText = cells[j]?.Trim() ?? string.Empty;

                    try
                    {
                        if (colName == COL_CONG_DOAN)
                        {
                            row.Cells[colName].Value = LayCongDoanIdTheoTen(cellText);
                        }
                        else
                        {
                            row.Cells[colName].Value = cellText;
                        }
                    }
                    catch (Exception ex)
                    {
                        int sttDong = row.Index + 1;
                        row.ErrorText = ex.Message;
                        dsLoi.Add($"Dòng {sttDong}: {ex.Message}");
                    }
                }
            }

            CapNhatSTT();

            if (dsLoi.Count > 0)
            {
                FrmWaiting.ShowGifAlert(string.Join(Environment.NewLine, dsLoi));
            }
        }

        private int LayCongDoanIdTheoTen(string tenCongDoan)
        {
            tenCongDoan = tenCongDoan?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(tenCongDoan))
                throw new InvalidOperationException("Tên công đoạn không được để trống khi paste.");

            var congDoan = ThongTinChungCongDoan.TatCaCongDoan
                .Where(x => x.Id != 2)
                .FirstOrDefault(x =>
                    string.Equals(
                        x.TenCongDoan?.Trim(),
                        tenCongDoan,
                        StringComparison.CurrentCultureIgnoreCase));

            if (congDoan == null)
                throw new InvalidOperationException($"Tên công đoạn không hợp lệ: {tenCongDoan}");

            return congDoan.Id;
        }

        private int LayViTriCotPaste(string colName)
        {
            int index = Array.IndexOf(_cotDuocPaste, colName);
            return index < 0 ? 0 : index;
        }

        private DataGridViewRow LayHoacTaoRow(int rowIndex)
        {
            while (rowIndex >= dgvDsNhapNVL.Rows.Count || dgvDsNhapNVL.Rows[rowIndex].IsNewRow)
            {
                dgvDsNhapNVL.Rows.Add();
            }

            return dgvDsNhapNVL.Rows[rowIndex];
        }

        private void dgvDsNhapNVL_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvDsNhapNVL.Columns[e.ColumnIndex].Name != COL_XOA) return;
            if (dgvDsNhapNVL.Rows[e.RowIndex].IsNewRow) return;

            DialogResult result = MessageBox.Show(
                "Bạn có chắc muốn xoá dòng này không?",
                "XÁC NHẬN XOÁ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                dgvDsNhapNVL.Rows.RemoveAt(e.RowIndex);
                CapNhatSTT();
            }
        }

        private void dgvDsNhapNVL_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            CapNhatSTT();
        }

        private void dgvDsNhapNVL_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            CapNhatSTT();
        }

        private void dgvDsNhapNVL_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            CapNhatSTT();
        }

        private void dgvDsNhapNVL_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            CapNhatSTT();
        }

        private void CapNhatSTT()
        {
            if (!dgvDsNhapNVL.Columns.Contains(COL_STT)) return;

            int sttValue = 1;
            foreach (DataGridViewRow row in dgvDsNhapNVL.Rows)
            {
                if (row.IsNewRow) continue;

                if (DongCoDuLieu(row))
                    row.Cells[COL_STT].Value = sttValue++;
                else
                    row.Cells[COL_STT].Value = null;
            }
        }

        private List<DataGridViewRow> LayDanhSachDongCoDuLieu()
        {
            return dgvDsNhapNVL.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && DongCoDuLieu(row))
                .ToList();
        }

        private bool DongCoDuLieu(DataGridViewRow row)
        {
            return row.Cells[COL_CONG_DOAN].Value != null
                || !string.IsNullOrWhiteSpace(LayCellText(row, COL_TEN))
                || !string.IsNullOrWhiteSpace(LayCellText(row, COL_KHOI_LUONG))
                || !string.IsNullOrWhiteSpace(LayCellText(row, COL_CHIEU_DAI))
                || !string.IsNullOrWhiteSpace(LayCellText(row, COL_QR))
                || !string.IsNullOrWhiteSpace(LayCellText(row, COL_GHI_CHU));
        }

        private NhapKhoNVL_Dong LayDuLieuTuGrid(DataGridViewRow row, bool batBuocCoId)
        {
            string ten = LayCellText(row, COL_TEN);
            if (string.IsNullOrWhiteSpace(ten))
                throw new InvalidOperationException("Tên sản phẩm không được để trống.");

            string khoiLuongText = LayCellText(row, COL_KHOI_LUONG);
            if (!TryParseDouble(khoiLuongText, out double khoiLuong))
                throw new InvalidOperationException("Khối lượng không hợp lệ.");

            string chieuDaiText = LayCellText(row, COL_CHIEU_DAI);
            double chieuDai = 0;
            if (!string.IsNullOrWhiteSpace(chieuDaiText) && !TryParseDouble(chieuDaiText, out chieuDai))
                throw new InvalidOperationException("Chiều dài không hợp lệ.");


            string qr = LayCellText(row, COL_QR);
            if (string.IsNullOrWhiteSpace(qr))
                throw new InvalidOperationException("Qr không được để trống. Vui lòng bấm Tạo Qr trước khi lưu.");



            long? id = null;
            if (row.Tag != null && long.TryParse(row.Tag.ToString(), out long tagId))
                id = tagId;

            if (batBuocCoId && (!id.HasValue || id.Value <= 0))
                throw new InvalidOperationException("Dòng này chưa có TTThanhPham_ID, không thể sửa.");

            int congDoanId = LayCongDoanIdTuGrid(row);

            return new NhapKhoNVL_Dong
            {
                TTThanhPhamId = id,
                Ten = ten.Trim(),
                TenKhongDau = CoreHelper.BoDauTiengViet(ten).Trim(),
                KhoiLuong = khoiLuong,
                ChieuDai = chieuDai,
                MaBin = qr,
                CongDoanId = congDoanId,
                GhiChu = LayCellText(row, COL_GHI_CHU)
            };
        }

        private void GanDongVaoGrid(DataGridViewRow row, NhapKhoNVL_Dong dong)
        {
            row.Cells[COL_CONG_DOAN].Value = dong.CongDoanId;
            row.Cells[COL_TEN].Value = dong.Ten;
            row.Cells[COL_KHOI_LUONG].Value = FormatSo(dong.KhoiLuong);
            row.Cells[COL_CHIEU_DAI].Value = FormatSo(dong.ChieuDai);
            row.Cells[COL_QR].Value = dong.MaBin;
            row.Cells[COL_GHI_CHU].Value = dong.GhiChu;
            row.Tag = dong.TTThanhPhamId;
        }

        private void XoaCacDongDaXuLy(List<DataGridViewRow> rows)
        {
            foreach (DataGridViewRow row in rows.OrderByDescending(r => r.Index))
            {
                if (row.DataGridView == dgvDsNhapNVL && !row.IsNewRow)
                    dgvDsNhapNVL.Rows.Remove(row);
            }
        }

        private void HoiVaInDanhSach(List<NhapKhoNVL_Dong> dsDong)
        {
            if (dsDong == null || dsDong.Count == 0) return;

            DialogResult result = MessageBox.Show(
                $"Đã lưu {dsDong.Count} thành công, bạn muốn in không?",
                "XÁC NHẬN IN TEM",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            List<string> dsLoiIn = new List<string>();
            foreach (NhapKhoNVL_Dong dong in dsDong)
            {
                try
                {
                    _dongDangIn = dong;
                    var printer = BuildPrinter();
                    PrintHelper.PrintLabel(printer);
                }
                catch (Exception ex)
                {
                    dsLoiIn.Add($"{dong.MaBin}: {ex.Message}");
                }
            }

            if (dsLoiIn.Count > 0)
            {
                FrmWaiting.ShowGifAlert(string.Join(Environment.NewLine, dsLoiIn));
            }
        }

        private int LayCongDoanIdTuGrid(DataGridViewRow row)
        {
            object value = row.Cells[COL_CONG_DOAN].Value;

            if (value == null || value == DBNull.Value)
                throw new InvalidOperationException("Vui lòng chọn công đoạn.");

            if (!int.TryParse(value.ToString(), out int congDoanId))
                throw new InvalidOperationException("Công đoạn không hợp lệ.");

            bool tonTai = ThongTinChungCongDoan.TatCaCongDoan
                .Any(x => x.Id == congDoanId && x.Id != 2);

            if (!tonTai)
                throw new InvalidOperationException("Công đoạn không hợp lệ hoặc không được phép chọn.");

            return congDoanId;
        }

        private PrinterModel BuildPrinter()
        {
            NhapKhoNVL_Dong dong = _dongDangIn ?? new NhapKhoNVL_Dong();

            return new PrinterModel
            {
                id = dong.TTThanhPhamId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                NgaySX = string.Empty,
                CaSX = string.Empty,
                Mau = string.Empty,
                KhoiLuong = FormatSo(dong.KhoiLuong),
                ChieuDai = FormatSo(dong.ChieuDai),
                TenSP = dong.Ten ?? string.Empty,
                QC = string.Empty,
                MaBin = dong.MaBin ?? string.Empty,
                MaSP = dong.MaSP ?? string.Empty,
                DanhGia = string.Empty,
                TenCN = string.Empty,
                GhiChu = dong.GhiChu ?? string.Empty
            };
        }

        private int LaySTTHienThi(DataGridViewRow row)
        {
            if (int.TryParse(LayCellText(row, COL_STT), out int sttValue) && sttValue > 0)
                return sttValue;

            return row.Index + 1;
        }

        private string LayCellText(DataGridViewRow row, string colName)
        {
            if (!dgvDsNhapNVL.Columns.Contains(colName)) return string.Empty;
            return row.Cells[colName].Value?.ToString()?.Trim() ?? string.Empty;
        }

        private bool TryParseDouble(string text, out double value)
        {
            value = 0;
            text = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return true;

            string normalized = text.Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private string FormatSo(double value)
        {
            return value.ToString("0.###", CultureInfo.CurrentCulture);
        }

        private void btnTaoQr_Click(object sender, EventArgs e)
        {
            dgvDsNhapNVL.EndEdit();
            CapNhatSTT();

            List<DataGridViewRow> dsRow = LayDanhSachDongCoDuLieu();
            if (dsRow.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập dữ liệu trước khi tạo QR.");
                return;
            }

            foreach (DataGridViewRow row in dsRow)
            {
                int sttDong = LaySTTHienThi(row);

                // plus được gán bằng STT của dòng
                int plus = sttDong;

                string maQr = NhapKhoNVL_DB.TaoQrNhapKhoNVL(FITST_CHARACTOR_QR, plus);

                row.Cells[COL_QR].Value = maQr;
            }
        }
    }
}
