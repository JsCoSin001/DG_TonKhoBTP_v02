using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using Control = System.Windows.Forms.Control;
using helperApp = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.UI.NghiepVu
{
    public partial class UC_KeHoach : UserControl
    {
        // Dùng để hiển thị chức năng theo bộ phận
        bool keHoachStaff = false;
        public UC_KeHoach()
        {
            InitializeComponent();      
            
            this.LoadChucNang(keHoachStaff);
        }

        private void LoadChucNang(bool banhanh)
        {
            if (banhanh)
            {
                BanHanhKH_SX banHanhKH_SX = new BanHanhKH_SX
                {
                    Dock = DockStyle.Fill,
                };
                pnChucNang.Controls.Add(banHanhKH_SX);
            }
            else
            {
                ThietLap_KH thietLap_KH = new ThietLap_KH
                {
                    Dock = DockStyle.Fill,
                };
                pnChucNang.Controls.Add(thietLap_KH);
            }
        }

        private void UC_KeHoach_Load(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            // Set thông số tìm kiếm
            SetThongSo(cbxTrangThaiThucHienKH, StoreKeyKeHoach.TrangThaiThucHienTheoKH);
            SetThongSo(cbxTinhTrangCuaKH, StoreKeyKeHoach.TrangThaiBanHanhKH);
            SetThongSo(cbxMucDoUuTienKH, StoreKeyKeHoach.MucDoUuTien);
        }

        private void SetThongSo(ComboBox cbx, StoreKeyKeHoach key, bool findData = true)
        {
            var dict0 = EnumStore.Get(key);

            int keyActive = dict0.Keys.Min();

            var dict = new Dictionary<int, string>();
            if (!findData)
            {
                foreach (var kv in dict0)
                {
                    if (kv.Key != -1)
                        dict[kv.Key] = kv.Value;
                }
                keyActive = dict0.Keys.Max();
            }
            else
            {
                dict = dict0;
            }

            // nếu combobox đang bind dữ liệu khác thì reset trước cho chắc
            cbx.DataSource = null;

            cbx.DataSource = new BindingSource(dict, null);
            cbx.DisplayMember = "Value"; // hiển thị chữ
            cbx.ValueMember = "Key";     // giá trị int
            cbx.SelectedValue = keyActive;

        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {

            TimKiemKeHoachSX k = LayDL_TimKiemKH();

            DataTable lsData = DatabaseHelper.SearchKeHoachSX_DataTable(k);

            // check lsdata null or empty
            if (lsData.Rows.Count == 0)
            {
                FrmWaiting.ShowGifAlert($"Không tìm thấy kết quả phù hợp.", "KẾT QUẢ TÌM KIẾM", EnumStore.Icon.Warning);
                return;
            }

            if (cbxXuatExcel.Checked)
            {
                ExcelExporter.Export(lsData, "BangThongHopKH");
                return;
            }


            dtgKetQuaTimKiemKH.AutoGenerateColumns = true;
            dtgKetQuaTimKiemKH.DataSource = null;
            dtgKetQuaTimKiemKH.DataSource = lsData;
            var col = dtgKetQuaTimKiemKH.Columns["GhiChu"];
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        }

        private  TimKiemKeHoachSX LayDL_TimKiemKH()
        {
            var f = new TimKiemKeHoachSX
            {
                // Ten: lấy theo combobox sản phẩm (hiển thị Ten)
                //Ten = Helper.TrimToNull(cbxTenSP.Text),

                Ten = helperApp.TrimToNull(cbxTenSP.Text),

                // NgayNhan / NgayGiao: DateTimePicker
                NgayNhan = helperApp.GetDateOrNull(dtNgayNhanKH),
                NgayGiao = helperApp.GetDateOrNull(dtNgayGiaoKH),

                // Lot
                Lot = helperApp.TrimToNull(cbxLot.Text),

                // Số lượng: NumericUpDown (0 => null)
                SLHangDat = nbHangDat.Value > 0 ? (double?)nbHangDat.Value : null,
                SLHangBan = nmHangBan.Value > 0 ? (double?)nmHangBan.Value : null,
                SLTong = nbTong.Value > 0 ? (double?)nbTong.Value : null,

                // Mau
                Mau = helperApp.TrimToNull(cbxMauSac.Text),

                

                // TenKhachHang
                TenKhachHang = helperApp.TrimToNull(cbxTenKhach.Text),

                // GhiChu
                GhiChu = helperApp.TrimToNull(tbGhiChu.Text),

                TinhTrangCuaKH = cbxTinhTrangCuaKH.SelectedIndex == 0 ? null: (int?)cbxTinhTrangCuaKH.SelectedIndex -1 ,
                

                MucDoUuTienKH = cbxMucDoUuTienKH.SelectedIndex == 0 ? null : (int?)cbxMucDoUuTienKH.SelectedIndex -1 ,

                TrangThaiThucHienKH = cbxTrangThaiThucHienKH.SelectedIndex == 0 ? null : (int?)cbxTrangThaiThucHienKH.SelectedIndex - 1,
            };


            return f;
        }

        private void ResetTimKiemControls()
        {
            // 1) ComboBox: bỏ chọn
            //cbxTrangThaiThucHienKH.SelectedIndex = -1;
            //cbxTinhTrangCuaKH.SelectedIndex = -1;
            //cbxMucDoUuTienKH.SelectedIndex = -1;
            //cbxLot.SelectedIndex = -1;
            ResetForm();

            // cbxTenSP là ComboBox: đưa về mặc định
            cbxTenSP.SelectedIndex = -1;
            cbxTenSP.Text = "";

            // 2) TextBox: clear
            tbGhiChu.Clear();
            cbxTenKhach.Clear();
            cbxMauSac.Clear();

            // 3) NumericUpDown: về 0
            nbTong.Value = 0;
            nmHangBan.Value = 0;
            nbHangDat.Value = 0;

            // 4) DateTimePicker: về mặc định
            var defaultDate = new DateTime(2000, 1, 1);
            dtNgayNhanKH.Value = defaultDate;
            dtNgayGiaoKH.Value = defaultDate;

            // 5) CheckBox: bỏ chọn
            cbxXuatExcel.Checked = false;

            // (Tuỳ chọn) đặt focus về control đầu tiên
            cbxTrangThaiThucHienKH.Focus();
        }


        private void btnClear_Click(object sender, EventArgs e)
        {
            dtgKetQuaTimKiemKH.DataSource = null;
            ResetTimKiemControls();
        }
    }
}
