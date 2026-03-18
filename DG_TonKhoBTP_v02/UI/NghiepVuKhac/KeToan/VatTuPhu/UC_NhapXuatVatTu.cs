using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;

namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan.VatTuPhu
{
    public partial class UC_NhapXuatVatTu : UserControl
    {
        private ComboBoxSearchHelper<string> _cbxTimDonHelper;
        private ComboBoxSearchHelper<string> _cbxTimTenHelper;

        bool _isNhapKho = true;

        public UC_NhapXuatVatTu(bool isNhapKho)
        {
            InitializeComponent();
            _isNhapKho = isNhapKho;
            lblTitle.Text = (isNhapKho ? "Nhập vật tư" : "Xuất vật tư").ToUpper();
        }

        // ────────────────────────────────────────────────────────────
        // 1. KHỞI TẠO GIAO DIỆN — dùng WaitingHelper bao quanh OnLoad
        // ────────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Chạy phần khởi tạo có thể chậm trong waiting
            _ = WaitingHelper.RunWithWaiting(async () =>
            {
                // KhoiTaoCbxTimDon / KhoiTaoCbxTimTen chỉ gán helper,
                // không gọi DB nên chạy trên UI thread là ổn —
                // nhưng bọc chung để giao diện nhất quán khi mở form.
                KhoiTaoCbxTimDon();
                KhoiTaoCbxTimTen();

                dgvChiTietDon.CellClick += DgvChiTietDon_CellClick;

                if (!_isNhapKho)
                {
                    dgvChiTietDon.Columns["yeuCau"].HeaderText = "Tồn Kho";
                    dgvChiTietDon.Columns["thucNhan"].HeaderText = "Số Lượng Xuất";
                }

                await Task.CompletedTask; // placeholder để đúng kiểu Func<Task>
            }, "ĐANG KHỞI TẠO GIAO DIỆN...");
        }

        private void KhoiTaoCbxTimTen()
        {
            Func<string, Task<List<string>>> searchFunc;
            if (_isNhapKho)
                searchFunc = keyword => DatabaseHelper.TimKiemTheoTenVatTu(keyword);
            else
                searchFunc = keyword => DatabaseHelper.TimKiemTheoTenVatTuConHang(keyword);

            _cbxTimTenHelper = new ComboBoxSearchHelper<string>(
                comboBox: cbxTimTen,
                searchFunc: searchFunc,
                displaySelector: ten => ten,
                onItemSelected: ten => _ = LoadChiTietDonTheoTenAsync(ten)
            );
        }

        // ────────────────────────────────────────────────────────────
        // 2. THAO TÁC DB — tải chi tiết đơn theo tên vật tư
        // ────────────────────────────────────────────────────────────
        private async Task LoadChiTietDonTheoTenAsync(string tenVatTu)
        {
            if (string.IsNullOrWhiteSpace(tenVatTu)) return;

            await WaitingHelper.RunWithWaiting(async () =>
            {
                var dt = _isNhapKho
                    ? await DatabaseHelper.LayChiTietDonTheoTenVatTu(tenVatTu)
                    : await DatabaseHelper.LayChiTietDonTheoTenVatTuXuatKho(tenVatTu);

                MergeVaoDgv(dt);
                _cbxTimTenHelper.Clear();
            }, "ĐANG TẢI DỮ LIỆU VẬT TƯ...");
        }

        private void DgvChiTietDon_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != dgvChiTietDon.Columns["xoa"].Index || e.RowIndex < 0)
                return;

            var confirm = MessageBox.Show(
                "Bạn có chắc muốn xóa dòng này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            if (dgvChiTietDon.DataSource is DataTable dt)
            {
                dt.Rows[e.RowIndex].Delete();
                dt.AcceptChanges();
            }
        }

        private void KhoiTaoCbxTimDon()
        {
            Func<string, Task<List<string>>> searchFunc;
            if (_isNhapKho)
                searchFunc = keyword => DatabaseHelper.TimKiemMaDon(keyword);
            else
                searchFunc = keyword => DatabaseHelper.TimKiemMaDonConHang(keyword);

            _cbxTimDonHelper = new ComboBoxSearchHelper<string>(
                comboBox: cbxTimDon,
                searchFunc: searchFunc,
                displaySelector: maDon => maDon,
                onItemSelected: maDon => _ = LoadChiTietDonAsync(maDon)
            );
        }

        private void MergeVaoDgv(DataTable dtMoi)
        {
            if (dtMoi == null || dtMoi.Rows.Count == 0) return;

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
        }

        // ────────────────────────────────────────────────────────────
        // 3. THAO TÁC DB — tải chi tiết đơn theo mã đơn
        // ────────────────────────────────────────────────────────────
        private async Task LoadChiTietDonAsync(string maDon)
        {
            if (string.IsNullOrWhiteSpace(maDon)) return;

            await WaitingHelper.RunWithWaiting(async () =>
            {
                var dt = _isNhapKho
                    ? await DatabaseHelper.LayChiTietDonDatHang(maDon)
                    : await DatabaseHelper.LayChiTietDonDatHangXuatKho(maDon);

                MergeVaoDgv(dt);
                _cbxTimDonHelper.Clear();
            }, "ĐANG TẢI CHI TIẾT ĐƠN...");
        }

        // ────────────────────────────────────────────────────────────
        // 4. THAO TÁC DB — lưu lịch sử xuất/nhập
        // ────────────────────────────────────────────────────────────
        private async void btnLuu_Click(object sender, EventArgs e)
        {
            if (dgvChiTietDon.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để lưu.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nguoiGiaoNhan = txtNguoiGiaoNhan.Text.Trim();
            string lyDoChung = txtLyDo.Text.Trim();
            string ngay = DateTime.Now.ToString("yyyy-MM-dd");

            if (string.IsNullOrWhiteSpace(nguoiGiaoNhan))
            {
                MessageBox.Show("Vui lòng nhập người giao/nhận.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNguoiGiaoNhan.Focus();
                return;
            }

            btnLuu.Enabled = false;

            string tenPhieu = null;

            await WaitingHelper.RunWithWaiting(async () =>
            {
                tenPhieu = await DatabaseHelper.LuuLichSuXuatNhap(
                    dgvChiTietDon,
                    nguoiGiaoNhan,
                    lyDoChung,
                    ngay,
                    _isNhapKho);
            }, _isNhapKho ? "ĐANG LƯU PHIẾU NHẬP KHO..." : "ĐANG LƯU PHIẾU XUẤT KHO...");

            btnLuu.Enabled = true;

            if (string.IsNullOrWhiteSpace(tenPhieu))
            {
                MessageBox.Show("Không có dòng nào hợp lệ để lưu.",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_isNhapKho)
                In_PhieuNhapKho(tenPhieu);
            else
                In_PhieuXuatKho(tenPhieu);

            MessageBox.Show($"Lưu thành công. Mã phiếu: {tenPhieu}",
                            "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ((DataTable)dgvChiTietDon.DataSource).Clear();
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
                NgayIn = DateTime.Now.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                So = "",
                Co = "",
                SoPhieu = tenPhieu,
                NguoiNhan = txtNguoiGiaoNhan.Text.Trim(),
                LyDoXuat = txtLyDo.Text.Trim(),
                XuatTaiKho = "Kho nguyên vật liệu",
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
                NgayIn = DateTime.Now.ToString("'Ngày' dd 'tháng' MM 'năm' yyyy"),
                SoPO = "",
                SoPhieu = tenPhieu,
                NguoiGiao = txtNguoiGiaoNhan.Text.Trim(),
                NhaCungCap = txtNguoiGiaoNhan.Text.Trim(),
                LyDoNhap = txtLyDo.Text.Trim(),
                KhoHang = "Kho nguyên vật liệu",
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
    }
}