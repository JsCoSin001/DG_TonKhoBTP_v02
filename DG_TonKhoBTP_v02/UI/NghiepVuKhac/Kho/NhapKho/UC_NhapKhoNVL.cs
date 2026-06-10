using DG_TonKhoBTP_v02.Database.Kho;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AppHelper = DG_TonKhoBTP_v02.Helper.Helper;

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


        private readonly string[] _cotDuocPaste = { COL_TEN, COL_KHOI_LUONG, COL_QR, COL_GHI_CHU };
        private NhapKhoNVL_Dong _dongDangIn;

        public UC_NhapKhoNVL()
        {
            InitializeComponent();
            CaiDatGridVaSuKien();
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

            dgvDsNhapNVL.KeyDown += dgvDsNhapNVL_KeyDown;
            dgvDsNhapNVL.CellContentClick += dgvDsNhapNVL_CellContentClick;
            dgvDsNhapNVL.CellValueChanged += dgvDsNhapNVL_CellValueChanged;
            dgvDsNhapNVL.RowsAdded += dgvDsNhapNVL_RowsAdded;
            dgvDsNhapNVL.RowsRemoved += dgvDsNhapNVL_RowsRemoved;
            dgvDsNhapNVL.UserDeletedRow += dgvDsNhapNVL_UserDeletedRow;
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

            if (maBin.Split('-')[0] != FITST_CHARACTOR_QR)
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
                FrmWaiting.ShowGifAlert(AppHelper.ShowErrorDatabase(ex, "TÌM QR"));
            }
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            dgvDsNhapNVL.EndEdit();

            List<DataGridViewRow> dsRow = LayDanhSachDongCoDuLieu();
            if (dsRow.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập dữ liệu cần lưu.");
                return;
            }

            List<DataGridViewRow> dsRowLuuOk = new List<DataGridViewRow>();
            List<NhapKhoNVL_Dong> dsDaLuu = new List<NhapKhoNVL_Dong>();
            List<string> dsLoi = new List<string>();

            foreach (DataGridViewRow row in dsRow)
            {
                row.ErrorText = string.Empty;

                int sttDong = LaySTTHienThi(row);
                try
                {
                    NhapKhoNVL_Dong input = LayDuLieuTuGrid(row, batBuocCoId: false);
                    NhapKhoNVL_Dong daLuu = NhapKhoNVL_DB.LuuMotDong(input, "X");

                    dsDaLuu.Add(daLuu);
                    dsRowLuuOk.Add(row);
                }
                catch (Exception ex)
                {
                    string loi = AppHelper.ShowErrorDatabase(ex, $"DÒNG {sttDong}");
                    row.ErrorText = loi;
                    dsLoi.Add($"Dòng {sttDong}: {loi}");
                }
            }

            XoaCacDongDaXuLy(dsRowLuuOk);
            CapNhatSTT();

            if (dsLoi.Count > 0)
            {
                FrmWaiting.ShowGifAlert(string.Join(Environment.NewLine, dsLoi));
            }

            if (dsDaLuu.Count > 0)
            {
                FrmWaiting.ShowGifAlert($"Đã lưu thành công {dsDaLuu.Count} dòng. Bạn có muốn in tem ngay bây giờ không?", "THÔNG BÁO", "question");

                HoiVaInDanhSach(dsDaLuu);
            }
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            dgvDsNhapNVL.EndEdit();

            List<DataGridViewRow> dsRow = LayDanhSachDongCoDuLieu();
            if (dsRow.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập hoặc tìm kiếm dữ liệu cần sửa.");
                return;
            }

            List<NhapKhoNVL_Dong> dsDaSua = new List<NhapKhoNVL_Dong>();
            List<string> dsLoi = new List<string>();

            foreach (DataGridViewRow row in dsRow)
            {
                row.ErrorText = string.Empty;

                int sttDong = LaySTTHienThi(row);
                try
                {
                    NhapKhoNVL_Dong input = LayDuLieuTuGrid(row, batBuocCoId: true);
                    NhapKhoNVL_Dong daSua = NhapKhoNVL_DB.CapNhatMotDong(input);

                    GanDongVaoGrid(row, daSua);
                    row.Tag = daSua.TTThanhPhamId;
                    dsDaSua.Add(daSua);
                }
                catch (Exception ex)
                {
                    string loi = AppHelper.ShowErrorDatabase(ex, $"DÒNG {sttDong}");
                    row.ErrorText = loi;
                    dsLoi.Add($"Dòng {sttDong}: {loi}");
                }
            }

            CapNhatSTT();

            if (dsLoi.Count > 0)
            {
                FrmWaiting.ShowGifAlert(string.Join(Environment.NewLine, dsLoi));
            }

            if (dsDaSua.Count > 0)
            {
                FrmWaiting.ShowGifAlert($"Đã sửa thành công {dsDaSua.Count} dòng.");
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

            for (int i = 0; i < lines.Length; i++)
            {
                DataGridViewRow row = LayHoacTaoRow(startRow + i);
                string[] cells = lines[i].Split('\t');

                for (int j = 0; j < cells.Length; j++)
                {
                    int pasteColIndex = startPasteColIndex + j;
                    if (pasteColIndex >= _cotDuocPaste.Length)
                        break;

                    string colName = _cotDuocPaste[pasteColIndex];
                    if (!dgvDsNhapNVL.Columns.Contains(colName))
                        continue;

                    row.Cells[colName].Value = cells[j]?.Trim();
                }
            }

            CapNhatSTT();
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
            return !string.IsNullOrWhiteSpace(LayCellText(row, COL_TEN))
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

            return new NhapKhoNVL_Dong
            {
                TTThanhPhamId = id,
                Ten = ten.Trim(),
                TenKhongDau = AppHelper.BoDauTiengViet(ten).Trim(),
                KhoiLuong = khoiLuong,
                ChieuDai = chieuDai,
                MaBin = qr,
                GhiChu = LayCellText(row, COL_GHI_CHU)
            };
        }

        private void GanDongVaoGrid(DataGridViewRow row, NhapKhoNVL_Dong dong)
        {
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
                $"Bạn có muốn in tem cho {dsDong.Count} dòng đã xử lý thành công không?",
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
