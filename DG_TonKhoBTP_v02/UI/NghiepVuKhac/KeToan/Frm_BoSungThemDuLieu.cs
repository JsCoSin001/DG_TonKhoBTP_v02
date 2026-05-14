using System;
using System.Data;
using System.Windows.Forms;
using DG_TonKhoBTP_v02.Database.KeToan;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.KeToan;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class Frm_BoSungThemDuLieu : Form
    {
        // ── State ────────────────────────────────────────────────────────────────
        private ComboBoxSearchHelper _tenSPSearchHelper;

        /// <summary>Id bản ghi TTBoSung đang được xem/sửa. 0 = chế độ thêm mới.</summary>
        private long _currentBoSungId = 0;

        /// <summary>Id của DanhSachMaSP đã chọn qua cbxTimTen.</summary>
        private long _selectedMaSP_ID = 0;

        // ════════════════════════════════════════════════════════════════════════
        // KHỞI TẠO
        // ════════════════════════════════════════════════════════════════════════

        public Frm_BoSungThemDuLieu()
        {
            InitializeComponent();
            InitTimTenSearch();

            this.Load += Frm_Load;
            this.btnLuu.Click += BtnLuu_Click;
            this.tbSua.Click += TbSua_Click;
            this.btnXoa.Click += BtnXoa_Click;
        }

        private void Frm_Load(object sender, EventArgs e)
        {
            SetEditMode(false);
            cbxTimTen.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        // COMBOBOX TÌM KIẾM TÊN SP (cbxTimTen)
        // ════════════════════════════════════════════════════════════════════════

        private void InitTimTenSearch()
        {
            _tenSPSearchHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimTen,
                queryFunc: BoSungThongTinSP_DB.TimKiemTenSPAsync
            );
            _tenSPSearchHelper.DisplayColumn = "Ten";
            _tenSPSearchHelper.ItemSelected += OnTenSPSelected;
            _tenSPSearchHelper.Cleared += OnTenSPCleared;
        }

        /// <summary>Người dùng chọn một sản phẩm từ dropdown cbxTimTen.</summary>
        private void OnTenSPSelected(DataRowView row)
        {
            // Lấy dữ liệu từ row vào biến local trước khi Reset() làm mất row
            long spId = long.TryParse(row["id"]?.ToString(), out long parsedId) ? parsedId : 0;
            string ma = row["Ma"]?.ToString() ?? string.Empty;
            string ten = row["Ten"]?.ToString() ?? string.Empty;

            // Lưu state
            _selectedMaSP_ID = spId;
            _currentBoSungId = 0;

            // Điền tbID, tbMa, tbTen TRƯỚC
            tbID.Text = spId > 0 ? spId.ToString() : string.Empty;
            tbMa.Text = ma;
            tbTen.Text = ten;

            // Xoá cbxTimTen qua BeginInvoke để không xung đột với event đang chạy.
            // Reset() sẽ gọi Cleared → OnTenSPCleared, nên cần tạm tắt rồi bật lại.
            BeginInvoke(new Action(() =>
            {
                _tenSPSearchHelper.Cleared -= OnTenSPCleared;
                _tenSPSearchHelper.Reset();
                _tenSPSearchHelper.Cleared += OnTenSPCleared;
            }));

            ClearDataControls();
            SetEditMode(false);

            // Bôi đen toàn bộ nội dung tbTenChiTiet và focus
            tbTenChiTiet.SelectAll();
            tbTenChiTiet.Focus();
        }

        private void OnTenSPCleared()
        {
            _selectedMaSP_ID = 0;
            tbID.Text = string.Empty;
            tbMa.Text = string.Empty;
            tbTen.Text = string.Empty;

            ClearDataControls();
            _currentBoSungId = 0;
            SetEditMode(false);
        }

        // ════════════════════════════════════════════════════════════════════════
        // TÌM KIẾM TTBOSUNG THEO TÊN SP (tbTimKiemTenSP + Enter)
        // ════════════════════════════════════════════════════════════════════════

        private void tbTimKiemTenSP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            string keyword = tbTimKiemTenSP.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(keyword)) return;

            try
            {
                BoSungThongTinSP_Model model = BoSungThongTinSP_DB.TimKiemBoSungTheoTenSP(keyword);

                if (model == null)
                {
                    FrmWaiting.ShowGifAlert("Không tìm thấy dữ liêu");
                    return;
                }

                _selectedMaSP_ID = model.DanhSachMaSP_ID;
                tbID.Text = model.DanhSachMaSP_ID > 0 ? model.DanhSachMaSP_ID.ToString() : string.Empty;
                tbMa.Text = model.Ma ?? string.Empty;
                tbTen.Text = model.Ten ?? string.Empty;

                FillDataControls(model);
                _currentBoSungId = model.Id;
                SetEditMode(true);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi tìm kiếm");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // LƯU (INSERT) — btnLuu
        // ════════════════════════════════════════════════════════════════════════

        private void BtnLuu_Click(object sender, EventArgs e)
        {
            if (!ValidateForSave()) return;

            try
            {
                BoSungThongTinSP_Model model = BuildModelFromControls();
                BoSungThongTinSP_DB.InsertBoSung(model);
                                
                FrmWaiting.ShowGifAlert("Lưu dữ liệu thành công!",myIcon: EnumStore.Icon.Success);

                ResetForm();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert($"Lỗi khi lưu dữ liệu:\n{ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SỬA (UPDATE) — tbSua
        // ════════════════════════════════════════════════════════════════════════

        private void TbSua_Click(object sender, EventArgs e)
        {
            if (_currentBoSungId <= 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có bản ghi nào được chọn để sửa.");
                return;
            }

            try
            {
                BoSungThongTinSP_Model model = BuildModelFromControls();
                model.Id = _currentBoSungId;
                BoSungThongTinSP_DB.UpdateBoSung(model);

                FrmWaiting.ShowGifAlert("Cập nhật dữ liệu thành công!", myIcon: EnumStore.Icon.Success);
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi cập nhật dữ liệu");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // XOÁ (DELETE) — btnXoa
        // ════════════════════════════════════════════════════════════════════════

        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (_currentBoSungId <= 0)
            {
                FrmWaiting.ShowGifAlert("Chưa có bản ghi nào được chọn để xoá.");
                return;
            }

            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn xoá bản ghi này?",
                "Xác nhận xoá", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                BoSungThongTinSP_DB.DeleteBoSung(_currentBoSungId);

                FrmWaiting.ShowGifAlert("Xoá bản ghi thành công!", myIcon: EnumStore.Icon.Success);

                ResetForm();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert("Lỗi khi xoá dữ liệu");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS — BUILD / FILL / CLEAR / VALIDATE
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Tạo BoSungThongTinSP_Model từ các TextBox trên form.</summary>
        private BoSungThongTinSP_Model BuildModelFromControls()
        {
            return new BoSungThongTinSP_Model
            {
                DanhSachMaSP_ID = _selectedMaSP_ID,
                TenChiTiet = tbTenChiTiet.Text.Trim(),
                TieuChuan = tbTieuChuan.Text.Trim(),
                Rph = tbRph.Text.Trim(),
                Rt = tbRt.Text.Trim(),
                Rtd = tbRtd.Text.Trim(),
                Rcd = tbRcd.Text.Trim(),
                Ca = tbCa.Text.Trim(),
                Is = tbIs.Text.Trim(),
                Istt = tbIstt.Text.Trim(),
                Sh = tbSh.Text.Trim(),
                No = tbNo.Text.Trim(),
                D = tbD.Text.Trim(),
                TNC = tbTNC.Text.Trim(),
                OD = tbOD.Text.Trim(),
                T = tbT.Text.Trim(),
                M = tbMau.Text.Trim(),
                KC = tbKetCau.Text.Trim(),
                LTDN = tbLtdn.Text.Trim(),
                GhiChu = tbGhiChu.Text.Trim(),
                LH = tbLH.Text.Trim(),
            };
        }

        /// <summary>Điền dữ liệu từ model vào các control trong tableLayoutPanel1 và tableLayoutPanel3.
        /// Không điền vào cbxTimTen, tbID, tbMa, tbTen.</summary>
        private void FillDataControls(BoSungThongTinSP_Model m)
        {
            // tableLayoutPanel1
            tbTenChiTiet.Text = m.TenChiTiet ?? string.Empty;
            tbTieuChuan.Text = m.TieuChuan ?? string.Empty;

            // tableLayoutPanel3
            tbRph.Text = m.Rph ?? string.Empty;
            tbRt.Text = m.Rt ?? string.Empty;
            tbRtd.Text = m.Rtd ?? string.Empty;
            tbRcd.Text = m.Rcd ?? string.Empty;
            tbCa.Text = m.Ca ?? string.Empty;
            tbIs.Text = m.Is ?? string.Empty;
            tbIstt.Text = m.Istt ?? string.Empty;
            tbSh.Text = m.Sh ?? string.Empty;
            tbNo.Text = m.No ?? string.Empty;
            tbD.Text = m.D ?? string.Empty;
            tbTNC.Text = m.TNC ?? string.Empty;
            tbOD.Text = m.OD ?? string.Empty;
            tbT.Text = m.T ?? string.Empty;
            tbMau.Text = m.M ?? string.Empty;
            tbKetCau.Text = m.KC ?? string.Empty;
            tbLtdn.Text = m.LTDN ?? string.Empty;
            tbGhiChu.Text = m.GhiChu ?? string.Empty;
            tbLH.Text = m.LH ?? string.Empty;
        }

        /// <summary>Xoá trắng toàn bộ các TextBox nhập liệu (tableLayoutPanel1 + tableLayoutPanel3).
        /// Không chạm cbxTimTen, tbID, tbMa, tbTen.</summary>
        private void ClearDataControls()
        {
            tbTenChiTiet.Text = string.Empty;
            tbTieuChuan.Text = string.Empty;

            tbRph.Text = string.Empty;
            tbRt.Text = string.Empty;
            tbRtd.Text = string.Empty;
            tbRcd.Text = string.Empty;
            tbCa.Text = string.Empty;
            tbIs.Text = string.Empty;
            tbIstt.Text = string.Empty;
            tbSh.Text = string.Empty;
            tbNo.Text = string.Empty;
            tbD.Text = string.Empty;
            tbTNC.Text = string.Empty;
            tbOD.Text = string.Empty;
            tbT.Text = string.Empty;
            tbMau.Text = string.Empty;
            tbKetCau.Text = string.Empty;
            tbLtdn.Text = string.Empty;
            tbGhiChu.Text = string.Empty;
            tbLH.Text = string.Empty;
        }

        /// <summary>Reset toàn bộ form về trạng thái ban đầu (sau lưu / sau xoá).</summary>
        private void ResetForm()
        {
            _currentBoSungId = 0;
            _selectedMaSP_ID = 0;

            _tenSPSearchHelper.Reset();   // xoá cbxTimTen
            tbID.Text = string.Empty;
            tbMa.Text = string.Empty;
            tbTen.Text = string.Empty;

            tbTimKiemTenSP.Text = string.Empty;

            ClearDataControls();
            SetEditMode(false);

            cbxTimTen.Focus();
        }

        /// <summary>
        /// false = chế độ thêm mới: btnLuu enabled, tbSua/btnXoa disabled.
        /// true  = chế độ xem/sửa:  tbSua/btnXoa enabled, btnLuu disabled.
        /// </summary>
        private void SetEditMode(bool isEditMode)
        {
            btnLuu.Enabled = !isEditMode;
            tbSua.Enabled = isEditMode;
            btnXoa.Enabled = isEditMode;
        }

        /// <summary>Kiểm tra điều kiện tối thiểu trước khi Insert.</summary>
        private bool ValidateForSave()
        {
            if (_selectedMaSP_ID <= 0)
            {

                FrmWaiting.ShowGifAlert("Vui lòng nhập Thông tin chi tiết");
                cbxTimTen.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbTenChiTiet.Text))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Thông tin chi tiết");
                tbTenChiTiet.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbTieuChuan.Text))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập Tiêu chuẩn");

                tbTieuChuan.Focus();
                return false;
            }

            return true;
        }

        // ════════════════════════════════════════════════════════════════════════
        // DISPOSE
        // ════════════════════════════════════════════════════════════════════════

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _tenSPSearchHelper?.Dispose();
            base.OnHandleDestroyed(e);
        }

        // ── Giữ lại stub được sinh bởi Designer ─────────────────────────────
        private void textBox4_TextChanged(object sender, EventArgs e) { }
    }
}