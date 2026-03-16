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
            lblTitle.Text = _KieuDon == 1 ? "ĐẶT HÀNG MUA VẬT TƯ": "ĐẶT HÀNG MUA DỊCH VỤ";

            SetupDGVColumns();
        }

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
            }
            else // _KieuDon == 2
            {
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ten", HeaderText = "Tên Vật Tư", Width = 500 });
                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "soLuong", HeaderText = "Số lượng", Width = 150 });

                var colMucDich = new DataGridViewTextBoxColumn { Name = "mucDich", HeaderText = "Mục đích" };
                colMucDich.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvDSMua.Columns.Add(colMucDich);

                dgvDSMua.Columns.Add(new DataGridViewTextBoxColumn { Name = "ngayGiao", HeaderText = "Ngày giao", Width = 150 });
                dgvDSMua.AllowUserToAddRows = true;
                //cbxTimTenVatTu.Enabled = false;
                //cbxTimTenVatTu.Text = "";
                //cbxTimTenVatTu.Items.Clear();

            }

            // Cột Xoá - chung cho cả 2 kiểu đơn
            dgvDSMua.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colXoa",
                HeaderText = "Xoá",
                Text = "X",
                UseColumnTextForButtonValue = true,
                Width = 50
            });
        }

        private async void UC_MuaVatTu_Load(object sender, EventArgs e)
        {
            var repo = new VatTuRepository();

            _vatTuSearchHelper = new ComboBoxSearchHelper<MuaVatTuSearchItem>(
                comboBox: cbxTimTenVatTu,
                searchFunc: keyword => repo.TimKiemTheoCheDoAsync(keyword, rdoTaoMoi.Checked),
                displaySelector: item => item.DisplayText,
                onItemSelected: item =>
                {
                    if (!item.IsDonHang)
                    {
                        dgvDSMua.Rows.Add(item.Id, item.Ma, item.Ten, item.DonVi);

                        cbxTimTenVatTu.Text = "";
                        cbxTimTenVatTu.Items.Clear();
                        return;
                    }

                    tbMaDon.Text = item.MaDon;
                    dgvDSMua.Rows.Clear();

                    var chiTietList = repo.GetChiTietDonHang(item.MaDon);

                    foreach (var ct in chiTietList)
                    {
                        if (!ct.DanhSachMaSP_ID.HasValue) continue;

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
                        );
                    }

                    cbxTimTenVatTu.Text = "";
                    cbxTimTenVatTu.Items.Clear();
                }
            );

            rdoTaoMoi.CheckedChanged += async (s, ev) => await CapNhatUITheoCheDoAsync();
            radioButton2.CheckedChanged += async (s, ev) => await CapNhatUITheoCheDoAsync();

            await CapNhatUITheoCheDoAsync();
        }

        private async Task CapNhatUITheoCheDoAsync()
        {
            bool taoMoiDon = rdoTaoMoi.Checked;

            cbxTimTenVatTu.Text = "";
            cbxTimTenVatTu.Items.Clear();

            // Nếu KieuDon == 2: Tạo mới thì disable cbx, Sửa đơn thì enable cbx
            // Nếu KieuDon == 1: cbx luôn enable (không đổi)
            if (_KieuDon == 2)
            {
                cbxTimTenVatTu.Enabled = !taoMoiDon; // Tạo mới → disable, Sửa đơn → enable
            }

            tbMaDon.Enabled = taoMoiDon;

            lblTieuDeTimKiem.Text = taoMoiDon ? "Tìm Tên Vật Tư": "Tìm theo Mã Đơn";

            if (taoMoiDon)
            {
                dgvDSMua.Rows.Clear();

                int soLuongDon = await Task.Run(() => DatabaseHelper.GetSoLuongDonThangHienTai(_KieuDon));
                string soDon = $"{GenerateMaDon(_KieuDon)}-{(soLuongDon + 1):0000}";
                tbMaDon.Text = soDon;
            }
            else
            {
                tbMaDon.Text = "";
            }

            
        }

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

        
        private string GenerateMaDon(int kieuDon = 1)
        {
            string don = kieuDon == 1 ? MaDonMuaVatTu : MaDonMuaDichVu;

            string y = DateTime.Now.Year.ToString();
            // get 2 last digits of year
                y = y.Substring(y.Length - 2);
            // get month and day with leading zero
            string m = DateTime.Now.Month.ToString("D2");
            
            don += y + "/" + m;

            return don;
        }


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
                bool daIn = false;

                if (taoMoiDon)
                {
                    vt.InsertDonDatHang(
                        new DanhSachDatHang { MaDon = maDon, LoaiDon = _KieuDon },
                        list
                    );

                    // Mặc định in
                    InPhieuMuaVatTu(maDon, list);
                    daIn = true;
                }
                else
                {
                    vt.UpdateDonDatHang(
                        new DanhSachDatHang { MaDon = maDon, LoaiDon = _KieuDon },
                        list
                    );

                    // Hỏi có in không
                    var hoiIn = MessageBox.Show(
                        "Bạn có muốn in lại phiếu mua vật tư không?",
                        "Xác nhận in",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (hoiIn == DialogResult.Yes)
                    {
                        InPhieuMuaVatTu(maDon, list);
                        daIn = true;
                    }
                }

                // Thông báo sau khi xử lý in
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

        private void InPhieuMuaVatTu(string maDon, List<ThongTinDatHang> list)
        {
            if (_KieuDon == 1)
            {
                var items = list.Select((item, index) => new MaterialItem
                {
                    No = index + 1,
                    MaterialCode = item.MaVatTu ?? "",
                    MaterialName = item.TenVatTu ?? "",
                    Unit = item.DonVi ?? "",
                    Quantity = item.SoLuongMua.HasValue
                                       ? item.SoLuongMua.Value.ToString("N2")
                                       : "",
                    UnitPrice = item.DonGia.HasValue
                                       ? item.DonGia.Value.ToString("N2")
                                       : "",
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
            else // _KieuDon == 2
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
                        Title = "GIẤY ĐỀ NGHỊ MUA DỊCH VỤ",
                        OrderDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        OrderCode = maDon
                    },
                    Items = items,
                    Signature = new SignatureInfo()
                };

                new PurchaseRequestPrintService(data).ShowPreview(this);
            }
        }


        private List<ThongTinDatHang> LayDuLieuTuDGV()
        {
            var list = new List<ThongTinDatHang>();

            foreach (DataGridViewRow row in dgvDSMua.Rows)
            {
                if (row.IsNewRow) continue;

                if (_KieuDon == 1)
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
                else // _KieuDon == 2
                {
                    list.Add(new ThongTinDatHang
                    {
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
    }
}
