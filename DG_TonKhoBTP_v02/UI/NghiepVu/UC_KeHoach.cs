using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace DG_TonKhoBTP_v02.UI.NghiepVu
{
    public partial class UC_KeHoach : UserControl
    {
        private Control _lastControl = null;

        private readonly System.Windows.Forms.Timer _debounce = new System.Windows.Forms.Timer { Interval = 250 };


        // Chặn TextChanged khi update UI bằng code (tránh loop)
        private bool _suppressTextChanged = false;

        // Chặn ValueChanged khi đang nạp dữ liệu (tránh tự tính ngoài ý muốn)
        private bool _suppressCalc = false;

        private void SafeUI(Action act)
        {
            _suppressTextChanged = true;
            try { act(); }
            finally { _suppressTextChanged = false; }
        }

        private void SafeCalc(Action act)
        {
            _suppressCalc = true;
            try { act(); }
            finally { _suppressCalc = false; }
        }

        public UC_KeHoach()
        {
            InitializeComponent();

            // BẮT SỰ KIỆN GÕ CHO COMBOBOX (quan trọng)
            cbTen.TextUpdate += AnyCombo_TextUpdate;
            tbLot.TextUpdate += AnyCombo_TextUpdate;

            _debounce.Tick += (s, e) =>
            {
                _debounce.Stop();
                if (_suppressTextChanged) return;

                if (_lastControl == cbTen)
                    TimSP(cbTen.Text.Trim());
                else if (_lastControl == tbLot)
                    TimKH(tbLot.Text.Trim());
            };

            ClearForm();
        }

        private void AnyCombo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressTextChanged) return;

            _lastControl = (Control)sender;
            _debounce.Stop();
            _debounce.Start();
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbTen.Text) || string.IsNullOrWhiteSpace(tbMa.Text))
            {
                FrmWaiting.ShowGifAlert("Tên hoặc Mã sản phẩm không phù hợp");
                cbTen.Focus();
                return;
            }

            if (tbTong.Value < tbHangDat.Value)
            {
                FrmWaiting.ShowGifAlert("Tổng hoặc khối lượng hàng bán không phù hợp");
                return;
            }

            if (tbLot.Text.Trim() == "")
            {
                FrmWaiting.ShowGifAlert("Vui lòng nhập LOT!");
                tbLot.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(dtNgayGiao.Text))
            {
                FrmWaiting.ShowGifAlert("Vui lòng chọn ngày giao!", "CẢNH BÁO");
                dtNgayGiao.Focus();
                return;
            }

            var keHoach = new KeHoachSX
            {
                Ma = tbMa.Text.Trim(),
                Ten = cbTen.Text.Trim(),
                DanhSachMaSP_ID = (int)tbIDMaSP.Value,
                NgayNhan = dtime.Value.ToString("yyyy-MM-dd"),
                Lot = tbLot.Text.Trim(),
                SLHangDat = tbHangDat.Value > 0 ? (double?)tbHangDat.Value : null,
                SLHangBan = tbHangBan.Value > 0 ? (double?)tbHangBan.Value : null,
                Mau = rtbMauSac.Text.Trim(),
                NgayGiao = dtNgayGiao.Value.ToString("yyyy-MM-dd"),
                TenKhachHang = tbTenKhachHang.Text.Trim(),
                GhiChu = rtbGhiChu.Text.Trim(),
                TinhTrangKH = cbxTinhTrangKH.SelectedIndex >= 0 ? cbxTinhTrangKH.SelectedIndex : 0,
                TinhTrangDon = cbxTinhTrangDon.SelectedIndex >= 0 ? cbxTinhTrangDon.SelectedIndex : 0,
                TrangThaiSX = 0,
            };

            int isNewPlan = KieuKH.SelectedIndex;

            DbResult result = isNewPlan == 0
                ? DatabaseHelper.InsertKeHoachSX(keHoach)
                : DatabaseHelper.UpdateKeHoachSX(keHoach);

            string mess = isNewPlan == 0 ? "Tạo mới: " : "Cập nhât: ";
            string status = (result.Ok ? "" : "Lỗi\n") + result.Message;
            string icon = result.Ok ? EnumStore.Icon.Success : EnumStore.Icon.Warning;

            FrmWaiting.ShowGifAlert(mess + status, "THÔNG BÁO", icon);

            if (result.Ok) ClearForm();
        }

        private void ClearForm()
        {
            SafeUI(() =>
            {
                SafeCalc(() =>
                {
                    tbMa.Clear();

                    cbTen.DataSource = null;
                    cbTen.Text = "";
                    tbIDMaSP.Value = 0;

                    tbLot.DataSource = null;
                    tbLot.Text = "";

                    tbTong.Value = 0;
                    tbHangBan.Value = 0;
                    tbHangDat.Value = 0;

                    rtbMauSac.Clear();
                    rtbGhiChu.Clear();
                    tbTenKhachHang.Clear();

                    cbxTinhTrangDon.SelectedIndex = 0;
                    KieuKH.SelectedIndex = 0;

                    dtime.Value = DateTime.Now;
                    dtNgayGiao.Value = DateTime.Now;
                    cbxTinhTrangKH.SelectedIndex = 0;

                });
            });
        }

        // ====== SEARCH SP ======
        private void TimSP(string t)
        {

            t = (t ?? "").Trim();
            if (t.Length == 0) { SafeUI(() => cbTen.DroppedDown = false); return; }

            int caret = cbTen.SelectionStart;

            var dt = DatabaseHelper.GetData(
                "SELECT Id, Ten, Ma FROM DanhSachMaSP WHERE Ten LIKE @kw AND Ma LIKE 'TP.%' ORDER BY Ten LIMIT 30",
                "%" + t + "%", "kw"
            );

            if (dt.Rows.Count == 0)
            {
                SafeUI(() =>
                {
                    cbTen.DroppedDown = false;
                    cbTen.DataSource = null;
                    tbIDMaSP.Text = "";
                    cbTen.Text = t;
                    cbTen.SelectionStart = Math.Min(caret, cbTen.Text.Length);
                });
                return;
            }

            SafeUI(() =>
            {
                cbTen.BeginUpdate();

                cbTen.DataSource = dt;
                cbTen.DisplayMember = "Ten";
                cbTen.ValueMember = "Id";
                cbTen.SelectedIndex = -1;

                cbTen.Text = t;
                cbTen.SelectionStart = Math.Min(caret, cbTen.Text.Length);

                cbTen.EndUpdate();
                cbTen.DroppedDown = true;
            });
        }

        private void cbTen_SelectionChangeCommitted(object sender, EventArgs e)
        {
            DataRowView r = cbTen.SelectedItem as DataRowView;
            if (r == null) return;

            _debounce.Stop();

            SafeUI(() =>
            {
                tbIDMaSP.Text = Convert.ToString(r["Id"]);
                tbMa.Text = Convert.ToString(r["Ma"]);

                cbTen.Text = Convert.ToString(r["Ten"]);
                cbTen.SelectionStart = cbTen.Text.Length;

                cbTen.DroppedDown = false;
            });
        }

        // ====== SEARCH LOT + LOAD PLAN ======

        private void TimKH(string kw)
        {
            // --- 1) Chỉ chạy khi KieuKH = 1 (ưu tiên SelectedValue, fallback SelectedIndex) ---
            int kieu = 0;
            if (KieuKH.SelectedValue != null && int.TryParse(KieuKH.SelectedValue.ToString(), out var v))
                kieu = v;
            else
                kieu = KieuKH.SelectedIndex;

            if (kieu != 1) return;

            // --- 2) Chuẩn hoá từ khoá ---
            kw = (kw ?? string.Empty).Trim();

            // Nếu rỗng thì đóng dropdown và clear datasource (tuỳ bạn muốn giữ hay không)
            if (kw.Length == 0)
            {
                if (tbLot.IsHandleCreated)
                {
                    tbLot.BeginInvoke(new Action(() =>
                    {
                        tbLot.DroppedDown = false;
                        tbLot.DataSource = null;
                    }));
                }
                else
                {
                    tbLot.DroppedDown = false;
                    tbLot.DataSource = null;
                }
                return;
            }

            // --- 3) Lưu caret để set lại sau khi đổi DataSource/Text ---
            int caret = tbLot.SelectionStart;

            // --- 4) Query DB  ---
            // Gợi ý: tìm theo LIKE, lấy top để list không quá dài.
            const string sql = @"
                SELECT  k.DanhSachMaSP_ID,
                        d.id  AS SP_ID,
                        d.Ten AS SP_Ten,
                        d.Ma  AS SP_Ma,
                        k.NgayNhan, k.Lot, k.SLHangDat, k.SLHangBan, k.Mau, k.NgayGiao, k.GhiChu,
                        k.TenKhachHang, k.TinhTrangKH, k.TinhTrangDon
                FROM KeHoachSX k
                LEFT JOIN DanhSachMaSP d ON d.id = k.DanhSachMaSP_ID
                WHERE k.Lot LIKE @kw
                ORDER BY k.Lot
                LIMIT 30;";

            // Nếu DatabaseHelper của bạn nhận tên param là "kw" thì giữ như vậy.
            var dt = DatabaseHelper.GetData(sql, "%" + kw + "%", "kw");

            // --- 5) Không có dữ liệu: đóng dropdown, giữ text người dùng ---
            if (dt == null || dt.Rows.Count == 0)
            {
                if (tbLot.IsHandleCreated)
                {
                    tbLot.BeginInvoke(new Action(() =>
                    {
                        tbLot.BeginUpdate();

                        tbLot.DroppedDown = false;
                        tbLot.DataSource = null;

                        tbLot.Text = kw;
                        tbLot.SelectionStart = Math.Min(caret, tbLot.Text.Length);

                        tbLot.EndUpdate();
                    }));
                }
                else
                {
                    tbLot.BeginUpdate();
                    tbLot.DroppedDown = false;
                    tbLot.DataSource = null;
                    tbLot.Text = kw;
                    tbLot.SelectionStart = Math.Min(caret, tbLot.Text.Length);
                    tbLot.EndUpdate();
                }

                return;
            }

            // --- 6) Có dữ liệu: bind và xổ dropdown ---
            Action bindAction = () =>
            {
                tbLot.BeginUpdate();

                // Gán datasource
                tbLot.DataSource = dt;
                tbLot.DisplayMember = "Lot";
                tbLot.ValueMember = "Lot";

                // Không auto chọn item đầu tiên
                tbLot.SelectedIndex = -1;

                // Trả lại đúng text người dùng đang gõ
                tbLot.Text = kw;
                tbLot.SelectionStart = Math.Min(caret, tbLot.Text.Length);

                tbLot.EndUpdate();

                // Mẹo: xổ dropdown sau (BeginInvoke) để WinForms không tự đóng lại
                tbLot.BeginInvoke(new Action(() =>
                {
                    // Tránh lỗi khi form chưa focus
                    if (tbLot.Focused)
                        tbLot.DroppedDown = true;
                }));
            };

            if (tbLot.IsHandleCreated)
                tbLot.BeginInvoke(bindAction);
            else
                bindAction();
        }

        //private void TimKH(string kw)
        //{

        //    if (KieuKH.SelectedIndex == 0) return;

        //    kw = (kw ?? "").Trim();
        //    if (kw.Length == 0) { SafeUI(() => tbLot.DroppedDown = false); return; }

        //    int caret = tbLot.SelectionStart;

        //    const string sql = @"
        //        SELECT  k.DanhSachMaSP_ID,
        //                d.id  AS SP_ID,
        //                d.Ten AS SP_Ten,
        //                d.Ma  AS SP_Ma,
        //                k.NgayNhan, k.Lot, k.SLHangDat, k.SLHangBan, k.Mau, k.NgayGiao, k.GhiChu,
        //                k.TenKhachHang, k.TinhTrangKH, k.TinhTrangDon
        //        FROM KeHoachSX k
        //        LEFT JOIN DanhSachMaSP d ON d.id = k.DanhSachMaSP_ID
        //        WHERE k.Lot LIKE @kw
        //        ORDER BY k.Lot
        //        LIMIT 30;";

        //    var dt = DatabaseHelper.GetData(sql, "%" + kw + "%", "kw");

        //    if (dt.Rows.Count == 0)
        //    {
        //        SafeUI(() =>
        //        {
        //            tbLot.DroppedDown = false;
        //            tbLot.DataSource = null;
        //            tbLot.Text = kw;
        //            tbLot.SelectionStart = Math.Min(caret, tbLot.Text.Length);
        //        });
        //        return;
        //    }

        //    SafeUI(() =>
        //    {
        //        tbLot.BeginUpdate();

        //        tbLot.DataSource = dt;
        //        tbLot.DisplayMember = "Lot";
        //        tbLot.ValueMember = "Lot";
        //        tbLot.SelectedIndex = -1;

        //        tbLot.Text = kw;
        //        tbLot.SelectionStart = Math.Min(caret, tbLot.Text.Length);

        //        tbLot.EndUpdate();
        //        tbLot.DroppedDown = true;
        //    });
        //}

        private void tbLot_SelectionChangeCommitted(object sender, EventArgs e)
        {
            DataRowView r = tbLot.SelectedItem as DataRowView;
            if (r == null) return;

            _debounce.Stop();

            SafeUI(() =>
            {

                // LOT
                tbLot.Text = Convert.ToString(r["Lot"]);
                tbLot.SelectionStart = tbLot.Text.Length;
                tbLot.DroppedDown = false;

                // SP
                tbIDMaSP.Text = Convert.ToString(r["DanhSachMaSP_ID"]);
                tbMa.Text = Convert.ToString(r["SP_Ma"]);

                // cbTen: bỏ datasource để set text tránh auto bind lại
                cbTen.DataSource = null;
                cbTen.Text = Convert.ToString(r["SP_Ten"]);
                cbTen.SelectionStart = cbTen.Text.Length;

                // Ngày
                DateTime ngayNhan, ngayGiao;
                if (DateTime.TryParse(Convert.ToString(r["NgayNhan"]), out ngayNhan))
                    dtime.Value = ngayNhan;
                else
                    dtime.Value = DateTime.Now;

                if (DateTime.TryParse(Convert.ToString(r["NgayGiao"]), out ngayGiao))
                    dtNgayGiao.Value = ngayGiao;
                else
                    dtNgayGiao.Value = DateTime.Now;

                // Text
                rtbMauSac.Text = Convert.ToString(r["Mau"]);
                rtbGhiChu.Text = Convert.ToString(r["GhiChu"]);
                tbTenKhachHang.Text = Convert.ToString(r["TenKhachHang"]);

                // Tình trạng
                int tinhTrangKH = ToIntOrZero(r["TinhTrangKH"]);
                int tinhTrangDon = ToIntOrZero(r["TinhTrangDon"]);

                cbxTinhTrangKH.SelectedIndex = ClampIndex(tinhTrangKH, cbxTinhTrangKH.Items.Count);
                cbxTinhTrangDon.SelectedIndex = ClampIndex(tinhTrangDon, cbxTinhTrangDon.Items.Count);

                // Số lượng (đảm bảo tbHangDat = tbTong - tbHangBan đúng theo dữ liệu)
                SafeCalc(() =>
                {
                    decimal slDat = ToDecimalOrZero(r["SLHangDat"]);
                    decimal slBan = ToDecimalOrZero(r["SLHangBan"]);

                    tbHangBan.Value = ClampDecimal(slBan, tbHangBan.Minimum, tbHangBan.Maximum);

                    decimal tong = slDat + slBan;
                    tbTong.Value = ClampDecimal(tong, tbTong.Minimum, tbTong.Maximum);

                });
            });
        }

        private static int ToIntOrZero(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            int x;
            return int.TryParse(Convert.ToString(v), out x) ? x : 0;
        }

        private static decimal ToDecimalOrZero(object v)
        {
            if (v == null || v == DBNull.Value) return 0m;
            decimal d;
            return decimal.TryParse(Convert.ToString(v), out d) ? d : 0m;
        }

        private static int ClampIndex(int idx, int count)
        {
            if (count <= 0) return -1;
            if (idx < 0) return 0;
            if (idx >= count) return count - 1;
            return idx;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ====== CALC ======       

        private void TinhKLConLai(object sender, EventArgs e)
        {
            decimal result = tbTong.Value - tbHangBan.Value;
            tbHangDat.Value = result < 0 ? 0 : result;

        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {

        }

        private void UC_KeHoach_Load(object sender, EventArgs e)
        {
            SetThongSo(cbxTinhTrangKH, StoreKeyKeHoach.TrangThaiBanHanhKH,false);
            SetThongSo(cbxTinhTrangDon, StoreKeyKeHoach.TrangThaiDonHang,false);

            // Set thông số tìm kiếm
            SetThongSo(cbxTrangThaiThucHien, StoreKeyKeHoach.TrangThaiThucHienTheoKH);
            SetThongSo(cbxTinhTrang, StoreKeyKeHoach.TrangThaiBanHanhKH);
            SetThongSo(cbxTinhTrangDonKH, StoreKeyKeHoach.TrangThaiDonHang);
        }

        private void SetThongSo(ComboBox cbx, StoreKeyKeHoach key, bool findData = true)
        {
            var dict0 = EnumStore.Get(key);


            var dict = new Dictionary<int, string>();
            if (!findData)
            {
                foreach (var kv in dict0)
                {
                    if (kv.Key != -1)
                        dict[kv.Key] = kv.Value;
                }
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
        }

    }
}
