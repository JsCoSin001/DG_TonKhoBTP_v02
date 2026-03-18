using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Printer;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DG_TonKhoBTP_v02.Printer.A4.A4Config;
using static DG_TonKhoBTP_v02.Printer.A4.A4Printer;
using static DG_TonKhoBTP_v02.Printer.A4.PrinterModel;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;


namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_MuaVatTu : UserControl
    {
        private ComboBoxSearchHelper<MuaVatTuSearchItem> _vatTuSearchHelper;

        private int _KieuDon = 1; // 1: Đơn mua vật tư, 2: Đơn dịch vụ

        // *** THÊM MỚI: cờ theo dõi layout DGV hiện tại ***
        // false  = layout đầy đủ (KieuDon==1 với ma, donVi, donGia, slTon…)
        // true   = layout rút gọn (ten, soLuong, mucDich, ngayGiao, xoa)
        private bool _isNullMaSPLayout = false;

        private string MaDonMuaDichVu => "PRS";
        private string MaDonMuaVatTu => "PRM";

        public UC_MuaVatTu(int kieuDon)
        {
            InitializeComponent();
            _KieuDon = kieuDon;
            setupUI();
        }

        private void setupUI()
        {
            lblTitle.Text = _KieuDon == 1 ? "ĐẶT HÀNG MUA VẬT TƯ" : "ĐẶT HÀNG MUA DỊCH VỤ";
            SetupDGVColumns();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // SETUP CỘT DGV
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Layout mặc định theo _KieuDon.
        /// KieuDon == 1: full columns (id, ma, ten, donVi, soLuong, donGia, mucDich, ngayGiao, slTon, xoa)
        /// KieuDon == 2: layout rút gọn (ten, soLuong, mucDich, ngayGiao, xoa)
        /// </summary>
        private void SetupDGVColumns()
        {
            dgvDSMua.Columns.Clear();

            if (_KieuDon == 1)
            {
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "ID", Width = 100 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ma", HeaderText = "Mã vật tư", Width = 150 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ten", HeaderText = "Tên Vật Tư", Width = 250 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "donVi", HeaderText = "Đơn Vị", Width = 100 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "soLuong", HeaderText = "Số lượng", Width = 150 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "donGia", HeaderText = "Đơn giá", Width = 100 });

                var colMucDich = new DataGridViewTextBoxColumn { Name = "mucDich", HeaderText = "Mục đích" };
                colMucDich.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvDSMua.Columns.Add(colMucDich);

                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ngayGiao", HeaderText = "Ngày giao", Width = 100 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "slTon", HeaderText = "Số lượng tồn", Width = 100 });
                dgvDSMua.AllowUserToAddRows = false;

                _isNullMaSPLayout = false;
                AddXoaColumn();
            }
            else // _KieuDon == 2
            {
                SetupDGVColumnsNullMaSP(allowAddRows: true);
            }
        }

        /// <summary>
        /// Layout rút gọn dùng khi DanhSachMaSP_ID = null (hoặc KieuDon == 2).
        /// ten | soLuong | mucDich | ngayGiao | xoa
        /// </summary>
        /// <param name="allowAddRows">true khi KieuDon==2 tạo mới, false khi edit đơn</param>
        private void SetupDGVColumnsNullMaSP(bool allowAddRows = false)
        {
            dgvDSMua.Columns.Clear();

            dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ten", HeaderText = "Tên", Width = 400 });
            dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "soLuong", HeaderText = "Số lượng", Width = 150 });

            var colMucDich = new DataGridViewTextBoxColumn { Name = "mucDich", HeaderText = "Mục đích" };
            colMucDich.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvDSMua.Columns.Add(colMucDich);

            dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ngayGiao", HeaderText = "Ngày giao", Width = 150 });
            dgvDSMua.AllowUserToAddRows = allowAddRows;

            AddXoaColumn();

            _isNullMaSPLayout = true;
        }

        private void AddXoaColumn()
        {
            dgvDSMua.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colXoa",
                HeaderText = "Xoá",
                Text = "X",
                UseColumnTextForButtonValue = true,
                Width = 50
            });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────────────────────────

        private async void UC_MuaVatTu_Load(object sender, EventArgs e)
        {
            var repo = new VatTuRepository();

            _vatTuSearchHelper = new ComboBoxSearchHelper<MuaVatTuSearchItem>(
                comboBox: cbxTimTenVatTu,
                searchFunc: keyword => repo.TimKiemTheoCheDoAsync(keyword, rdoTaoMoi.Checked, _KieuDon),
                displaySelector: item => item.DisplayText,
                onItemSelected: item =>
                {
                    // ── Chế độ TẠO MỚI: item là vật tư từ DanhSachMaSP ──
                    if (!item.IsDonHang)
                    {
                        // Chỉ áp dụng cho KieuDon == 1 (layout đầy đủ)
                        dgvDSMua.Rows.Add(item.Id, item.Ma, item.Ten, item.DonVi);
                        cbxTimTenVatTu.Text = "";
                        cbxTimTenVatTu.Items.Clear();
                        return;
                    }

                    // ── Chế độ SỬA ĐƠN: item là mã đơn hàng ──
                    tbMaDon.Text = item.MaDon;
                    dgvDSMua.Rows.Clear();

                    var chiTietList = repo.GetChiTietDonHang(item.MaDon);
                    if (chiTietList == null || chiTietList.Count == 0)
                    {
                        cbxTimTenVatTu.Text = "";
                        cbxTimTenVatTu.Items.Clear();
                        return;
                    }

                    // Xác định layout dựa trên dữ liệu thực tế của đơn hàng
                    bool coNullMaSP = chiTietList.Any(ct => !ct.DanhSachMaSP_ID.HasValue);

                    if (coNullMaSP)
                    {
                        // Layout rút gọn: không cho thêm dòng tự do khi đang edit
                        SetupDGVColumnsNullMaSP(allowAddRows: false);
                        LoadDGVNullMaSP(chiTietList);
                    }
                    else
                    {
                        // Đảm bảo layout đầy đủ (KieuDon == 1)
                        if (_isNullMaSPLayout)
                            SetupDGVColumns();

                        LoadDGVFullColumns(chiTietList, repo);
                    }

                    cbxTimTenVatTu.Text = "";
                    cbxTimTenVatTu.Items.Clear();
                }
            );

            rdoTaoMoi.CheckedChanged += async (s, ev) => await CapNhatUITheoCheDoAsync();
            radioButton2.CheckedChanged += async (s, ev) => await CapNhatUITheoCheDoAsync();

            await CapNhatUITheoCheDoAsync();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // LOAD DỮ LIỆU VÀO DGV
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Điền dữ liệu vào DGV với layout đầy đủ (KieuDon == 1, DanhSachMaSP_ID có giá trị).
        /// Cột: id | ma | ten | donVi | soLuong | donGia | mucDich | ngayGiao | slTon | xoa
        /// </summary>
        private void LoadDGVFullColumns(List<ThongTinDatHang> chiTietList, VatTuRepository repo)
        {
            foreach (var ct in chiTietList)
            {
                if (!ct.DanhSachMaSP_ID.HasValue) continue; // bỏ qua dòng không có mã SP

                var vt = repo.GetVatTuById(ct.DanhSachMaSP_ID.Value);
                if (vt == null) continue;

                dgvDSMua.Rows.Add(
                    vt.Id,
                    vt.Ma,
                    vt.Ten,
                    vt.DonVi,
                    ct.SoLuongMua,
                    ct.DonGia,
                    ct.MucDichMua,
                    ct.NgayGiao
                // slTon để trống khi load edit (không lưu trong DB)
                );
            }
        }

        /// <summary>
        /// Điền dữ liệu vào DGV với layout rút gọn (DanhSachMaSP_ID = null).
        /// Cột: ten | soLuong | mucDich | ngayGiao | xoa
        /// </summary>
        private void LoadDGVNullMaSP(List<ThongTinDatHang> chiTietList)
        {
            foreach (var ct in chiTietList)
            {
                dgvDSMua.Rows.Add(
                    ct.TenVatTu,
                    ct.SoLuongMua,
                    ct.MucDichMua,
                    ct.NgayGiao
                );
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // CẬP NHẬT UI THEO CHẾ ĐỘ
        // ─────────────────────────────────────────────────────────────────────────────

        private async Task CapNhatUITheoCheDoAsync()
        {
            bool taoMoiDon = rdoTaoMoi.Checked;

            cbxTimTenVatTu.Text = "";
            cbxTimTenVatTu.Items.Clear();

            if (_KieuDon == 2)
            {
                cbxTimTenVatTu.Enabled = !taoMoiDon; // Tạo mới → disable, Sửa đơn → enable
            }

            tbMaDon.Enabled = taoMoiDon;
            lblTieuDeTimKiem.Text = taoMoiDon ? "Tìm Tên Vật Tư" : "Tìm theo Mã Đơn";

            if (taoMoiDon)
            {
                dgvDSMua.Rows.Clear();

                // *** Reset layout về mặc định khi chuyển sang chế độ tạo mới ***
                // Tránh trường hợp trước đó đang edit đơn có NullMaSP layout
                if (_isNullMaSPLayout && _KieuDon == 1)
                    SetupDGVColumns();
                else if (_KieuDon == 2 && !_isNullMaSPLayout)
                    SetupDGVColumnsNullMaSP(allowAddRows: true);

                int soLuongDon = await Task.Run(() => DatabaseHelper.GetSoLuongDonThangHienTai(_KieuDon));
                string soDon = $"{GenerateMaDon(_KieuDon)}-{(soLuongDon + 1):0000}";
                tbMaDon.Text = soDon;
            }
            else
            {
                dgvDSMua.Rows.Clear();
                tbMaDon.Text = "";
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // SỰ KIỆN DGV
        // ─────────────────────────────────────────────────────────────────────────────

        private void dgvDSMua_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvDSMua.Columns["colXoa"].Index && e.RowIndex >= 0)
            {
                var confirm = MessageBox.Show("Bạn có chắc muốn xóa dòng này?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                    dgvDSMua.Rows.RemoveAt(e.RowIndex);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // ĐẶT HÀNG
        // ─────────────────────────────────────────────────────────────────────────────

        private void btnDatHang_Click(object sender, EventArgs e)
        {
            List<ThongTinDatHang> list = LayDuLieuTuDGV();
            string maDon = CoreHelper.TrimToNull(tbMaDon.Text);

            if (string.IsNullOrEmpty(maDon))
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập mã đơn hàng trước khi đặt hàng!");
                return;
            }
            if (list.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Danh sách vật tư đang trống");
                return;
            }

            VatTuRepository vt = new VatTuRepository();
            bool taoMoiDon = rdoTaoMoi.Checked;

            try
            {
                if (taoMoiDon)
                {
                    vt.InsertDonDatHang(
                        new DanhSachDatHang { MaDon = maDon, LoaiDon = _KieuDon },
                        list
                    );
                    InPhieuMuaVatTu(maDon, list);
                }
                else
                {
                    vt.UpdateDonDatHang(
                        new DanhSachDatHang { MaDon = maDon, LoaiDon = _KieuDon },
                        list
                    );

                    var hoiIn = MessageBox.Show(
                        "Bạn có muốn in lại phiếu mua vật tư không?",
                        "Xác nhận in",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (hoiIn == DialogResult.Yes)
                        InPhieuMuaVatTu(maDon, list);
                }

                FrmWaiting.ShowGifAlert(
                    $"{(taoMoiDon ? "Tạo" : "Cập nhật")} đơn hàng thành công: {maDon}",
                    "ĐẶT HÀNG",
                    EnumStore.Icon.Success
                );

                dgvDSMua.Rows.Clear();
                cbxTimTenVatTu.Focus();
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(
                    CoreHelper.ShowErrorDatabase(ex, maDon),
                    "ĐẶT HÀNG"
                );
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // ĐỌC DỮ LIỆU TỪ DGV
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Đọc dữ liệu từ DGV dựa trên layout hiện tại (_isNullMaSPLayout).
        /// Layout đầy đủ  → đọc: id, ma, ten, donVi, soLuong, donGia, mucDich, ngayGiao, slTon
        /// Layout rút gọn → đọc: ten, soLuong, mucDich, ngayGiao (DanhSachMaSP_ID = null)
        /// </summary>
        private List<ThongTinDatHang> LayDuLieuTuDGV()
        {
            var list = new List<ThongTinDatHang>();

            foreach (DataGridViewRow row in dgvDSMua.Rows)
            {
                if (row.IsNewRow) continue;

                if (!_isNullMaSPLayout) // layout đầy đủ (KieuDon == 1 với mã SP)
                {
                    list.Add(new ThongTinDatHang
                    {
                        DanhSachMaSP_ID = CoreHelper.TryParseInt(row.Cells["id"].Value),
                        MaVatTu = CoreHelper.TrimToNull(row.Cells["ma"].Value?.ToString()),
                        TenVatTu = CoreHelper.TrimToNull(row.Cells["ten"].Value?.ToString()),
                        DonVi = CoreHelper.TrimToNull(row.Cells["donVi"].Value?.ToString()),
                        SoLuongMua = CoreHelper.TryParseDecimal(row.Cells["soLuong"].Value),
                        DonGia = CoreHelper.TryParseDecimal(row.Cells["donGia"].Value),
                        MucDichMua = CoreHelper.TrimToNull(row.Cells["mucDich"].Value?.ToString()),
                        NgayGiao = CoreHelper.TrimToNull(row.Cells["ngayGiao"].Value?.ToString()),
                        SoLuongTon = CoreHelper.TrimToNull(row.Cells["slTon"].Value?.ToString()),
                        DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                else // layout rút gọn (DanhSachMaSP_ID = null)
                {
                    list.Add(new ThongTinDatHang
                    {
                        DanhSachMaSP_ID = null,
                        TenVatTu = CoreHelper.TrimToNull(row.Cells["ten"].Value?.ToString()),
                        SoLuongMua = CoreHelper.TryParseDecimal(row.Cells["soLuong"].Value),
                        MucDichMua = CoreHelper.TrimToNull(row.Cells["mucDich"].Value?.ToString()),
                        NgayGiao = CoreHelper.TrimToNull(row.Cells["ngayGiao"].Value?.ToString()),
                        DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            return list;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // IN PHIẾU
        // ─────────────────────────────────────────────────────────────────────────────

        private void InPhieuMuaVatTu(string maDon, List<ThongTinDatHang> list)
        {
            if (_KieuDon == 1 && !_isNullMaSPLayout)
            {
                var items = list.Select((item, index) => new MaterialItem
                {
                    No = index + 1,
                    MaterialCode = item.MaVatTu ?? "",
                    MaterialName = item.TenVatTu ?? "",
                    Unit = item.DonVi ?? "",
                    Quantity = item.SoLuongMua.HasValue ? item.SoLuongMua.Value.ToString("N2") : "",
                    UnitPrice = item.DonGia.HasValue ? item.DonGia.Value.ToString("N2") : "",
                    Purpose = item.MucDichMua ?? "",
                    RequiredDate = item.NgayGiao ?? "//",
                    CurrentStock = item.SoLuongTon ?? "",
                }).ToList();

                var data = new MaterialRequestPrintData
                {
                    Company = new CompanyInfo(),
                    Document = new DocumentInfo
                    {
                        Title = "GIẤY ĐỀ NGHỊ MUA VẬT TƯ",
                        OrderDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        OrderCode = maDon
                    },
                    Items = items,
                    Signature = new SignatureInfo()
                };

                new MaterialRequestPrintService(data).ShowPreview(this);
            }
            else // KieuDon == 2 hoặc NullMaSP layout
            {
                var items = list.Select((item, index) => new ServiceItem
                {
                    No = index + 1,
                    ServiceName = item.TenVatTu ?? "",
                    Purpose = item.MucDichMua ?? "",
                    RequiredDate = item.NgayGiao ?? "//",
                }).ToList();

                var data = new PurchaseRequestPrintData
                {
                    Company = new CompanyInfo
                    {
                        FormCode = "BM-12-04",
                        IssueDate = "05/05/2009"
                    },
                    Document = new DocumentInfo
                    {
                        Title = _KieuDon == 1 ? "GIẤY ĐỀ NGHỊ MUA VẬT TƯ" : "GIẤY ĐỀ NGHỊ MUA DỊCH VỤ",
                        OrderDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        OrderCode = maDon
                    },
                    Items = items,
                    Signature = new SignatureInfo()
                };

                new PurchaseRequestPrintService(data).ShowPreview(this);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // TIỆN ÍCH
        // ─────────────────────────────────────────────────────────────────────────────

        private string GenerateMaDon(int kieuDon = 1)
        {
            string don = kieuDon == 1 ? MaDonMuaVatTu : MaDonMuaDichVu;
            string y = DateTime.Now.Year.ToString();
            y = y.Substring(y.Length - 2);
            string m = DateTime.Now.Month.ToString("D2");
            don += y + "/" + m;
            return don;
        }
    }
}