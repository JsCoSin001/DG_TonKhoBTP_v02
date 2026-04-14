using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper;
// [THÊM] namespace của ComboBoxSearchHelper mới (non-generic, dùng DataTable + event)
// [ĐÃ XOÁ] ComboBoxSearchHelper<T> generic cũ nằm trong DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox
// namespace khớp với vị trí file: UI\Helper\AutoSearchWithCombobox\ComboBoxSearchHelper.cs
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    public partial class UC_NhapXuatVatTu : UserControl
    {
        // ── FIELD ──────────────────────────────────────────────────────────
        // [ĐÃ SỬA] ComboBoxSearchHelper<string> → ComboBoxSearchHelper (non-generic, dùng DataTable + event)
        private ComboBoxSearchHelper _cbxTimDonHelper;
        // [ĐÃ SỬA] ComboBoxSearchHelper<string> → ComboBoxSearchHelper (non-generic, dùng DataTable + event)
        private ComboBoxSearchHelper _cbxTimTenHelper;
        // [ĐÃ SỬA] ComboBoxSearchHelper<string> → ComboBoxSearchHelper (non-generic, dùng DataTable + event)
        private ComboBoxSearchHelper _cbxNhaCungCapHelper;
        private bool IsEdit => cbxKieu.SelectedIndex == 1;
        private bool IsKhac => !rdoLoai.Checked;
        private string NguoiLam => tbxnguoiLam.Text;

        bool _isNhapKho = true;
        int _kieu = 1;
        DataTable _khoList;

        // kieu = 1: Vật tư
        // kieu = 2: Dịch vụ
        public UC_NhapXuatVatTu(bool isNhapKho, int kieu, DataTable khoList)
        {
            InitializeComponent();
            _isNhapKho = isNhapKho;
            _kieu = kieu;
            _khoList = khoList;

            string label = "Nhập vật tư";
            cbxKieu.SelectedIndex = 0;

            if (isNhapKho)
            {
                label = kieu == 2 ? EnumStore.TieuDeFormVatTu.DON_XAC_NHAN_DICH_VU
                        : EnumStore.TieuDeFormVatTu.NHAP_VAT_TU;

                dgvChiTietDon.Columns["donGia"].Visible = true;
                dgvChiTietDon.Columns["thanhTien"].Visible = true;
            }
            else
            {
                label = EnumStore.TieuDeFormVatTu.XUAT_VAT_TU;
            }

            lblTitle.Text = label.ToUpper();
        }

        // ────────────────────────────────────────────────────────────
        // 1. KHỞI TẠO GIAO DIỆN — dùng WaitingHelper bao quanh OnLoad
        // ────────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _ = WaitingHelper.RunWithWaiting(async () =>
            {
                KhoiTaoCbxTimDon();

                KhoiTaoCbxTimTen();

                KhoiTaoGiaoDienKhac();

                TimKiemTenNCC();

                dgvChiTietDon.CellClick -= DgvChiTietDon_CellClick;
                dgvChiTietDon.CellClick += DgvChiTietDon_CellClick;

                if (!_isNhapKho)
                {
                    dgvChiTietDon.Columns["yeuCau"].HeaderText = "Tồn Kho";
                    dgvChiTietDon.Columns["thucNhan"].HeaderText = "Số Lượng Xuất";
                }


                dgvChiTietDon.CellFormatting -= DgvChiTietDon_CellFormatting;
                dgvChiTietDon.CellFormatting += DgvChiTietDon_CellFormatting;

                dgvChiTietDon.CellValidating -= DgvChiTietDon_CellValidating;
                dgvChiTietDon.CellValidating += DgvChiTietDon_CellValidating;

                dgvChiTietDon.CellParsing -= DgvChiTietDon_CellParsing;
                dgvChiTietDon.CellParsing += DgvChiTietDon_CellParsing;


                await Task.CompletedTask;
            }, "ĐANG KHỞI TẠO GIAO DIỆN...");
        }

        private void DgvChiTietDon_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvChiTietDon.Columns[e.ColumnIndex].Name != "ngay") return;
            if (e.Value == null || e.Value == DBNull.Value) return;

            string s = e.Value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(s)) return;

            DateTime dt;
            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "dd-MM-yyyy"
            };

            if (DateTime.TryParseExact(
                s,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dt))
            {
                e.Value = dt.ToString("dd-MM-yyyy");
                e.FormattingApplied = true;
            }
        }

        private void DgvChiTietDon_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvChiTietDon.Columns[e.ColumnIndex].Name != "ngay") return;

            string input = e.FormattedValue?.ToString().Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                dgvChiTietDon.Rows[e.RowIndex].ErrorText = "Ngày không được để trống.";
                e.Cancel = true;
                return;
            }

            DateTime dt;
            bool ok = DateTime.TryParseExact(
                input,
                "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dt);

            if (!ok)
            {
                dgvChiTietDon.Rows[e.RowIndex].ErrorText = "Ngày phải có định dạng dd-MM-yyyy.";
                FrmWaiting.ShowGifAlert("Ngày phải có định dạng dd-MM-yyyy.", myIcon: EnumStore.Icon.Warning);
                e.Cancel = true;
                return;
            }

            dgvChiTietDon.Rows[e.RowIndex].ErrorText = "";
        }

        private void DgvChiTietDon_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvChiTietDon.Columns[e.ColumnIndex].Name != "ngay") return;
            if (e.Value == null) return;

            string input = e.Value.ToString().Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            DateTime dt;
            if (DateTime.TryParseExact(
                input,
                "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dt))
            {
                // Lưu vào DataTable / SQLite dưới dạng string yyyy-MM-dd
                e.Value = dt.ToString("yyyy-MM-dd");
                e.ParsingApplied = true;
            }
        }

        private void KhoiTaoGiaoDienKhac()
        {
            cbxKhoHang.DataSource = _khoList;
            cbxKhoHang.DisplayMember = "TenKho";   // text hiển thị
            cbxKhoHang.ValueMember = "id";     // giá trị lấy ra khi chọn

            cbxKhoHang.SelectedIndex = -1;
            cbxKhoHang.Text = "";


        }

        // ── THÊM MỚI: Khởi tạo tìm kiếm trong cbxNhaCungCap ─────────────
        // Đặt sau KhoiTaoGiaoDienKhac, nhóm cùng các hàm KhoiTao khác
        private void TimKiemTenNCC()
        {
            // [ĐÃ SỬA] Bỏ constructor generic cũ (searchFunc trả List<string>, displaySelector, onItemSelected).
            // Dùng ComboBoxSearchHelper mới: queryFunc trả DataTable, callback qua event ItemSelected.
            _cbxNhaCungCapHelper = new ComboBoxSearchHelper(
                comboBox: cbxNhaCungCap,
                queryFunc: async (keyword, ct) =>
                {
                    // [ĐÃ SỬA] Bọc List<string> trả về từ hàm cũ vào DataTable 1 cột "Value"
                    // để khớp API mới. Logic gọi DB (DatabaseHelper.TimKiemNhaCungCap) giữ nguyên.
                    var list = await DatabaseHelper.TimKiemNhaCungCap(keyword);
                    ct.ThrowIfCancellationRequested();
                    return ListStringToDataTable(list, "Value");
                }
            );
            // [ĐÃ SỬA] DisplayColumn khớp tên cột "Value" trong DataTable wrapper.
            _cbxNhaCungCapHelper.DisplayColumn = "Value";
            // [ĐÃ SỬA] Đăng ký callback qua event thay vì truyền onItemSelected vào constructor.
            // [GIỮ NGUYÊN] Logic trong OnNhaCungCapSelected không thay đổi.
            _cbxNhaCungCapHelper.ItemSelected += row => OnNhaCungCapSelected(row["Value"]?.ToString());
        }

        // ── THÊM MỚI: Xử lý khi người dùng chọn nhà cung cấp ────────────
        // Đặt ngay sau KhoiTaoCbxNhaCungCap
        private void OnNhaCungCapSelected(string nhaCungCap)
        {
            // Giá trị người dùng chọn đã tự động điền vào cbxNhaCungCap.Text
            // Nếu cần xử lý thêm (lọc dữ liệu, ghi log...) thì thêm vào đây
        }

        // ────────────────────────────────────────────────────────────
        // (Các hàm còn lại giữ nguyên hoàn toàn)
        // ────────────────────────────────────────────────────────────

        private void KhoiTaoCbxTimTen()
        {
            // [ĐÃ SỬA] Bỏ Func<string, Task<List<string>>> + constructor generic.
            // Dùng ComboBoxSearchHelper mới: queryFunc trả DataTable, callback qua event ItemSelected.
            // [GIỮ NGUYÊN] Logic chọn hàm theo _isNhapKho / IsEdit / IsKhac giữ nguyên.
            _cbxTimTenHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimTen,
                queryFunc: async (keyword, ct) =>
                {
                    List<string> list;
                    if (_isNhapKho)
                        list = await DatabaseHelper.TimKiemTheoTenVatTu(keyword, _kieu, IsEdit, IsKhac);
                    else
                        list = await DatabaseHelper.TimKiemTheoTenVatTuConHang(keyword);

                    ct.ThrowIfCancellationRequested();
                    return ListStringToDataTable(list, "Value");
                }
            );
            // [ĐÃ SỬA] DisplayColumn khớp tên cột "Value" trong DataTable wrapper.
            _cbxTimTenHelper.DisplayColumn = "Value";
            // [ĐÃ SỬA] Đăng ký callback qua event. [GIỮ NGUYÊN] logic gọi LoadChiTietDonTheoTenAsync.
            _cbxTimTenHelper.ItemSelected += row => _ = LoadChiTietDonTheoTenAsync(row["Value"]?.ToString());
        }

        private void KhoiTaoCbxTimDon()
        {
            // [ĐÃ SỬA] Bỏ Func<string, Task<List<string>>> + constructor generic.
            // Dùng ComboBoxSearchHelper mới: queryFunc trả DataTable, callback qua event ItemSelected.
            // [GIỮ NGUYÊN] Logic chọn hàm theo _isNhapKho / IsEdit giữ nguyên.
            _cbxTimDonHelper = new ComboBoxSearchHelper(
                comboBox: cbxTimDon,
                queryFunc: async (keyword, ct) =>
                {
                    List<string> list;
                    if (_isNhapKho)
                        list = await DatabaseHelper.TimKiemMaDon(keyword, IsEdit);
                    else
                        list = await DatabaseHelper.TimKiemMaDonConHang(keyword, IsEdit);

                    ct.ThrowIfCancellationRequested();
                    return ListStringToDataTable(list, "Value");
                }
            );
            // [ĐÃ SỬA] DisplayColumn khớp tên cột "Value" trong DataTable wrapper.
            _cbxTimDonHelper.DisplayColumn = "Value";
            // [ĐÃ SỬA] Đăng ký callback qua event. [GIỮ NGUYÊN] logic gọi LoadChiTietDonAsync.
            _cbxTimDonHelper.ItemSelected += row => _ = LoadChiTietDonAsync(row["Value"]?.ToString(), IsEdit);
        }

        private async Task LoadChiTietDonTheoTenAsync(string tenVatTu)
        {
            if (string.IsNullOrWhiteSpace(tenVatTu)) return;

            await WaitingHelper.RunWithWaiting(async () =>
            {
                DataTable dt;

                if (IsEdit)
                {
                    if (string.IsNullOrWhiteSpace(NguoiLam))
                    {
                        FrmWaiting.ShowGifAlert("Cần nhập người làm trước");
                        return;
                    }

                    dt = await DatabaseHelper.GetDataTuTenVatTuXuatNhap_Edit(tenVatTu, NguoiLam, _isNhapKho);
                }
                else
                {
                    dt = _isNhapKho
                        ? await DatabaseHelper.LayChiTietDonTheoTenVatTu(tenVatTu, _kieu, IsKhac)
                        : await DatabaseHelper.LayChiTietDonTheoTenVatTuXuatKho(tenVatTu);
                }

                MergeVaoDgv(dt, IsKhac);
                // [ĐÃ SỬA] Clear() → Reset() theo API mới của ComboBoxSearchHelper
                //_cbxTimTenHelper.Reset();
            }, "ĐANG TẢI DỮ LIỆU VẬT TƯ...");
        }

        private void DgvChiTietDon_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != dgvChiTietDon.Columns["xoa"].Index || e.RowIndex < 0)
                return;


            if (dgvChiTietDon.DataSource is DataTable dt)
            {
                // ✅ Lấy ID trước khi xóa
                int id = Convert.ToInt32(dt.Rows[e.RowIndex]["id"]);

                Console.WriteLine(IsEdit);
                // ✅ Xóa DB trước (nếu đang edit)
                if (IsEdit)
                {

                    var confirm = MessageBox.Show(
                        "Hành động này sẽ xóa dữ liệu trong hệ thống!",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes) return;

                    try
                    {
                        DatabaseHelper.Delete_ByID("LichSuXuatNhap", id);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xóa DB: " + ex.Message);
                        return;
                    }
                }
                else
                {
                    var confirm = MessageBox.Show(
                        "Bạn có chắc chắn xóa dòng này",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes) return;
                }

                // ✅ Xóa trên DataTable
                dt.Rows[e.RowIndex].Delete();
                dt.AcceptChanges();
            }
        }


        private void MergeVaoDgv(DataTable dtMoi, bool isKhac, bool isEdit = false)
        {
            if (dtMoi == null || dtMoi.Rows.Count == 0) return;
            bool showMa_DonVi = _kieu == 1;

            fl.Enabled = false;

            if (dgvChiTietDon.DataSource is DataTable dtHienTai)
            {
                var idsHienCo = new HashSet<long>(
                       dtHienTai.Rows
                                .Cast<DataRow>()
                                .Select(r => Convert.ToInt64(r["id"]))
                   );

                foreach (DataRow row in dtMoi.Rows)
                {
                    long id = Convert.ToInt64(row["id"]);
                    if (!idsHienCo.Contains(id))
                        dtHienTai.ImportRow(row);
                }

                dtHienTai.AcceptChanges();
            }
            else
            {
                dgvChiTietDon.AutoGenerateColumns = false;
                dgvChiTietDon.DataSource = dtMoi;
            }

            dgvChiTietDon.Columns["ma"].Visible = showMa_DonVi;
            dgvChiTietDon.Columns["donVi"].Visible = showMa_DonVi;

            dgvChiTietDon.Columns["MaDon"].ReadOnly = true;
            dgvChiTietDon.Columns["donGia"].Visible = _isNhapKho;
            dgvChiTietDon.Columns["thanhTien"].Visible = _isNhapKho;
            dgvChiTietDon.Columns["yeuCau"].Visible = !isEdit;

            dgvChiTietDon.Columns["ngay"].DefaultCellStyle.Format = "dd-MM-yyyy";
        }

        private async Task LoadChiTietDonAsync(string maDon, bool isEdit)
        {
            if (string.IsNullOrWhiteSpace(maDon)) return;

            await WaitingHelper.RunWithWaiting(async () =>
            {
                var dt = _isNhapKho
                    ? await DatabaseHelper.LayChiTietDonDatHang(maDon, IsEdit)
                    : await DatabaseHelper.LayChiTietDonDatHangXuatKho(maDon, IsEdit);

                MergeVaoDgv(dt, IsKhac);
                // [ĐÃ SỬA] Clear() → Reset() theo API mới của ComboBoxSearchHelper
                //_cbxTimDonHelper.Reset();
            }, "ĐANG TẢI CHI TIẾT ĐƠN...");
        }

        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (dgvChiTietDon.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để lưu.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cbxKhoHang.Text == "" && !IsEdit && _kieu != 2)
            {
                MessageBox.Show("Kho hàng không được rỗng", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            string nguoiGiaoNhan = txtNguoiGiaoNhan.Text.Trim();
            string lyDoChung = rdoLoai.Checked ? "Theo đề nghị" : "Khác";
            string ngay = dtNgayNhapXuat.Value.ToString("yyyy-MM-dd");
            int kho = cbxKhoHang.SelectedIndex + 1;
            string nguoiLam = tbxnguoiLam.Text.Trim();

            if (string.IsNullOrWhiteSpace(nguoiLam))
            {
                MessageBox.Show("Vui lòng nhập người làm.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNguoiGiaoNhan.Focus();
                return;
            }

            btnLuu.Enabled = false;

            string tenPhieu = null;

            await WaitingHelper.RunWithWaiting(async () =>
            {


                if (!IsEdit)
                {

                    if (IsKhac)
                    {
                        var info = new DonKhacInfo
                        {
                            NguoiDat = nguoiLam,
                            NguoiGiaoNhan = nguoiGiaoNhan,
                            LyDoChung = lyDoChung,
                            Ngay = dtNgayNhapXuat.Value,
                            KhoId = kho,
                            NguoiLam = nguoiLam,
                            IsNhapKho = _isNhapKho
                        };

                        var items = LayDanhSachDonKhacItems(dgvChiTietDon);

                        tenPhieu = await DatabaseHelper.LuuDonKhacAsync(info, items);
                    }
                    else
                    {
                        tenPhieu = await DatabaseHelper.LuuLichSuXuatNhap(
                                              dgvChiTietDon,
                                              nguoiGiaoNhan,
                                              lyDoChung,
                                              ngay,
                                              kho, nguoiLam,
                                              _isNhapKho);

                        if (_kieu == 2)
                        {
                            await DatabaseHelper.LuuLichSuXuatNhap(
                                dgvChiTietDon,
                                nguoiGiaoNhan,
                                lyDoChung,
                                ngay,
                                kho, nguoiLam,
                                false);
                        }
                    }

                }
                else
                {
                    await DatabaseHelper.CapNhatLichSuXuatNhap(
                        dgvChiTietDon,
                        nguoiGiaoNhan,
                        lyDoChung, nguoiLam, _isNhapKho);
                }

            }, _isNhapKho
             ? "ĐANG LƯU PHIẾU NHẬP KHO..."
             : "ĐANG LƯU PHIẾU XUẤT KHO...");

            btnLuu.Enabled = true;

            if (string.IsNullOrWhiteSpace(tenPhieu) && !IsEdit)
            {
                FrmWaiting.ShowGifAlert("Lưu thất bại. Vui lòng thử lại.");
                return;
            }

            if (_isNhapKho)
                In_PhieuNhapKho(tenPhieu);
            else
                In_PhieuXuatKho(tenPhieu);

            FrmWaiting.ShowGifAlert("Lưu thành công", myIcon: EnumStore.Icon.Success);

            Reset();

        }

        private void Reset()
        {
            if (dgvChiTietDon.DataSource is DataTable dt)
                dt.Clear();

            txtNguoiGiaoNhan.Text = "";
            tbxnguoiLam.SelectedIndex = -1;
            tbxnguoiLam.Text = "";

            cbxKhoHang.SelectedIndex = -1;
            cbxKhoHang.Text = "";

            // [ĐÃ SỬA] Clear() → Reset() theo API mới của ComboBoxSearchHelper
            //_cbxTimDonHelper?.Reset();
            //_cbxTimTenHelper?.Reset();
            //_cbxNhaCungCapHelper?.Reset();
        }

        private void In_PhieuXuatKho(string tenPhieu)
        {
            if (dgvChiTietDon.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để in.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var items = new List<WarehouseIssuesItem>();

            for (int i = 0; i < dgvChiTietDon.Rows.Count; i++)
            {
                var row = dgvChiTietDon.Rows[i];
                if (row.IsNewRow) continue;

                items.Add(new WarehouseIssuesItem
                {
                    No = i + 1,
                    Ten = row.Cells["ten"]?.Value?.ToString() ?? "",
                    Ma = row.Cells["ma"]?.Value?.ToString() ?? "",
                    DonVi = row.Cells["donVi"]?.Value?.ToString() ?? "",
                    SoLuong = row.Cells["thucNhan"]?.Value?.ToString() ?? "",
                    DonGia = "",
                    ThanhTien = "",
                    GhiChu = dgvChiTietDon.Columns.Contains("ghiChu")
                                    ? row.Cells["ghiChu"]?.Value?.ToString() ?? ""
                                    : ""
                });
            }

            var data = new WarehouseIssuesPrintData
            {
                NgayIn = dtNgayNhapXuat.Value.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                So = "",
                Co = "",
                SoPhieu = tenPhieu,
                NguoiNhan = txtNguoiGiaoNhan.Text.Trim(),
                LyDoXuat = rdoLoai.Checked ? "Theo đề nghị" : "Khác",
                XuatTaiKho = cbxKhoHang.Text,
                Items = items,
                Signature = new SignatureInfo
                {
                    CheckerTitle = "Kế toán",
                    RequesterTitle = "Người nhận hàng",
                    FactoryDirectorTitle = "Thủ kho",
                    DirectorTitle = "Giám đốc"
                }
            };

            new WarehouseIssuesPrintService(data).ShowPreview(this);
        }


        public static List<DonKhacItem> LayDanhSachDonKhacItems(DataGridView dgvChiTietDon)
        {
            var result = new List<DonKhacItem>();

            if (dgvChiTietDon == null || dgvChiTietDon.Rows.Count == 0)
                return result;

            foreach (DataGridViewRow row in dgvChiTietDon.Rows)
            {
                if (row.IsNewRow) continue;

                object slObj = row.Cells["thucNhan"]?.Value;
                if (slObj == null || slObj == DBNull.Value) continue;
                if (!decimal.TryParse(slObj.ToString(), out decimal soLuong)) continue;
                if (soLuong == 0) continue;

                long? danhSachMaSpId = null;
                if (dgvChiTietDon.Columns.Contains("id"))
                {
                    object idObj = row.Cells["id"]?.Value;
                    if (idObj != null && idObj != DBNull.Value &&
                        long.TryParse(idObj.ToString(), out long tempId))
                    {
                        danhSachMaSpId = tempId;
                    }
                }

                string tenVatTu = null;
                if (dgvChiTietDon.Columns.Contains("tenVatTu"))
                    tenVatTu = row.Cells["tenVatTu"]?.Value?.ToString()?.Trim();
                else if (dgvChiTietDon.Columns.Contains("TenVatTu"))
                    tenVatTu = row.Cells["TenVatTu"]?.Value?.ToString()?.Trim();
                else if (dgvChiTietDon.Columns.Contains("ten"))
                    tenVatTu = row.Cells["ten"]?.Value?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(tenVatTu))
                    continue;

                string tenVatTuKhongDau = CoreHelper.BoDauTiengViet(tenVatTu);

                string mucDichMua = null;
                if (dgvChiTietDon.Columns.Contains("mucDichMua"))
                    mucDichMua = row.Cells["mucDichMua"]?.Value?.ToString()?.Trim();
                else if (dgvChiTietDon.Columns.Contains("MucDichMua"))
                    mucDichMua = row.Cells["MucDichMua"]?.Value?.ToString()?.Trim();

                string ghiChu = null;
                if (dgvChiTietDon.Columns.Contains("ghiChu"))
                    ghiChu = row.Cells["ghiChu"]?.Value?.ToString()?.Trim();
                else if (dgvChiTietDon.Columns.Contains("GhiChu"))
                    ghiChu = row.Cells["GhiChu"]?.Value?.ToString()?.Trim();

                decimal? donGia = null;
                if (dgvChiTietDon.Columns.Contains("DonGia"))
                {
                    object dgObj = row.Cells["DonGia"]?.Value;
                    if (dgObj != null && dgObj != DBNull.Value &&
                        decimal.TryParse(dgObj.ToString(), out decimal tempDonGia))
                    {
                        donGia = tempDonGia;
                    }
                }

                result.Add(new DonKhacItem
                {
                    DanhSachMaSpId = danhSachMaSpId,
                    TenVatTu = tenVatTu,
                    TenVatTuKhongDau = tenVatTuKhongDau,
                    SoLuong = soLuong,
                    MucDichMua = mucDichMua,
                    GhiChu = ghiChu,
                    DonGia = donGia
                });
            }

            return result;
        }

        private void In_PhieuNhapKho(string tenPhieu)
        {
            if (dgvChiTietDon.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để in.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var items = new List<WarehouseReceiptItem>();

            for (int i = 0; i < dgvChiTietDon.Rows.Count; i++)
            {
                var row = dgvChiTietDon.Rows[i];
                if (row.IsNewRow) continue;

                items.Add(new WarehouseReceiptItem
                {
                    No = i + 1,
                    Ten = row.Cells["ten"]?.Value?.ToString() ?? "",
                    Ma = row.Cells["ma"]?.Value?.ToString() ?? "",
                    DonVi = row.Cells["donVi"]?.Value?.ToString() ?? "",
                    YeuCau = row.Cells["yeuCau"]?.Value?.ToString() ?? "",
                    ThucNhan = row.Cells["thucNhan"]?.Value?.ToString() ?? "",
                    GhiChu = dgvChiTietDon.Columns.Contains("ghiChu")
                        ? row.Cells["ghiChu"]?.Value?.ToString() ?? ""
                        : ""
                });
            }

            var data = new WarehouseReceiptPrintData
            {
                NgayIn = dtNgayNhapXuat.Value.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                SoPO = "",
                SoPhieu = tenPhieu,
                NguoiGiao = txtNguoiGiaoNhan.Text.Trim(),
                NhaCungCap = cbxNhaCungCap.Text.Trim(),    // ← ĐÃ SỬA: lấy từ cbxNhaCungCap thay vì txtNguoiGiaoNhan
                LyDoNhap = rdoLoai.Checked ? "Theo đề nghị" : "Khác",
                KhoHang = cbxKhoHang.Text,
                Items = items,
                Signature = new SignatureInfo
                {
                    FactoryDirectorTitle = "Giám đốc nhà máy",
                    CheckerTitle = "Kế toán",
                    RequesterTitle = "Thủ kho"
                }
            };

            new WarehouseReceiptPrintService(data).ShowPreview(this);
        }


        private void dgvChiTietDon_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvChiTietDon.Rows[e.RowIndex];

            if (dgvChiTietDon.Columns[e.ColumnIndex].Name == "donGia" ||
                dgvChiTietDon.Columns[e.ColumnIndex].Name == "thucNhan")
            {
                try
                {
                    decimal dongia = 0;
                    decimal thucnhan = 0;

                    if (row.Cells["donGia"].Value != null)
                        decimal.TryParse(row.Cells["donGia"].Value.ToString(), out dongia);

                    if (row.Cells["thucNhan"].Value != null)
                        decimal.TryParse(row.Cells["thucNhan"].Value.ToString(), out thucnhan);

                    row.Cells["thanhTien"].Value = dongia * thucnhan;
                }
                catch
                {
                    // tránh crash nếu nhập sai
                }
            }
        }

        private void rdoLoai_CheckedChanged(object sender, EventArgs e)
        {
            bool flg = rdoLoai.Checked;

            //dgvChiTietDon.AllowUserToAddRows = !flg;
            //dgvChiTietDon.ReadOnly = flg;


            //foreach (DataGridViewColumn col in dgvChiTietDon.Columns)
            //{
            //    col.ReadOnly = flg;
            //}

            Console.WriteLine(flg);
        }

        private void ResetDataGridView(DataGridView dgvChiTietDon)
        {
            if (dgvChiTietDon.DataSource != null)
                dgvChiTietDon.DataSource = null;

            dgvChiTietDon.Rows.Clear();
            dgvChiTietDon.Refresh();
        }

        private void cbxKieu_SelectedIndexChanged(object sender, EventArgs e)
        {
            fl.Enabled = cbxKieu.SelectedIndex == 0;


            dgvChiTietDon.Columns["ngay"].Visible = IsEdit;

            ResetDataGridView(dgvChiTietDon);
        }

        // ── THÊM MỚI: Wrapper chuyển List<string> → DataTable 1 cột ─────────────────
        // Dùng chung cho _cbxTimDonHelper, _cbxTimTenHelper, _cbxNhaCungCapHelper.
        // ComboBoxSearchHelper mới yêu cầu queryFunc trả DataTable;
        // các hàm DB cũ (TimKiemMaDon, TimKiemTheoTenVatTu, TimKiemNhaCungCap)
        // vẫn trả List<string> nên cần bọc lại thay vì sửa chúng.
        // columnName phải khớp với DisplayColumn đã đặt trên helper tương ứng.
        private static DataTable ListStringToDataTable(List<string> list, string columnName)
        {
            var dt = new DataTable();
            dt.Columns.Add(columnName, typeof(string));

            if (list != null)
            {
                foreach (var item in list)
                    dt.Rows.Add(item);
            }

            return dt;
        }
        // ── HẾT THÊM MỚI ─────────────────────────────────────────────────────────────
    }
}