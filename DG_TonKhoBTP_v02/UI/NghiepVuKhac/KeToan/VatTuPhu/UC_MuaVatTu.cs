using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.Helper;
using DG_TonKhoBTP_v02.UI.Helper.AutoSearchWithCombobox;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;


namespace DG_TonKhoBTP_v02.UI.NghiepVuKhac.KeToan
{
    public partial class UC_MuaVatTu : UserControl
    {
        private ComboBoxSearchHelper<MuaVatTuSearchItem> _vatTuSearchHelper;

        private int _KieuDon = 1; // 1: Đơn mua vật tư, 2: Đơn dịch vụ
        public UC_MuaVatTu(int kieuDon = 1)
        {
            InitializeComponent();
            _KieuDon = kieuDon;
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

            if (taoMoiDon)
            {
                dgvDSMua.Rows.Clear();

                var repo = new VatTuRepository();

                int soLuongDon = await Task.Run(() => DatabaseHelper.GetSoLuongDonThangHienTai());

                string soDon = $"{ GenerateMaDon(_KieuDon)}-{(soLuongDon + 1):0000}";
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
            string don = kieuDon == 1 ? "PRM" : "PRS";

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

            VatTuRepository vt = new VatTuRepository();

            if (list.Count == 0)
            {
                FrmWaiting.ShowGifAlert("Danh sách vật tư đang trống");
                return;
            }

            bool taoMoiDon = rdoTaoMoi.Checked;

            try
            {
                if (taoMoiDon)
                {
                    vt.InsertDonDatHang(
                        new DanhSachDatHang
                        {
                            MaDon = maDon,
                            LoaiDon = _KieuDon
                        },
                        list
                    );

                    FrmWaiting.ShowGifAlert(
                        $"Tạo đơn hàng thành công: {maDon}",
                        "ĐẶT HÀNG",
                        EnumStore.Icon.Success
                    );
                }
                else
                {
                    vt.UpdateDonDatHang(
                        new DanhSachDatHang
                        {
                            MaDon = maDon,
                            LoaiDon = _KieuDon
                        },
                        list
                    );

                    FrmWaiting.ShowGifAlert(
                        $"Cập nhật đơn hàng thành công: {maDon}",
                        "ĐẶT HÀNG",
                        EnumStore.Icon.Success
                    );
                }
            }
            catch (Exception ex)
            {
                FrmWaiting.ShowGifAlert(CoreHelper.ShowErrorDatabase(ex, maDon), "ĐẶT HÀNG");
            }
        }

        private List<ThongTinDatHang> LayDuLieuTuDGV()
        {
            var list = new List<ThongTinDatHang>();

            foreach (DataGridViewRow row in dgvDSMua.Rows)
            {
                if (row.IsNewRow) continue;

                list.Add(new ThongTinDatHang
                {
                    DanhSachMaSP_ID = CoreHelper.TryParseInt(row.Cells["id"].Value),

                    SoLuongMua = CoreHelper.TryParseDecimal(row.Cells["soLuong"].Value),
                    DonGia = CoreHelper.TryParseDecimal(row.Cells["donGia"].Value),

                    MucDichMua = CoreHelper.TrimToNull(row.Cells["mucDich"].Value?.ToString()),
                    NgayGiao = CoreHelper.TrimToNull(row.Cells["ngayGiao"].Value?.ToString()),
                    DateInsert = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }

            return list;
        }
    }
}
