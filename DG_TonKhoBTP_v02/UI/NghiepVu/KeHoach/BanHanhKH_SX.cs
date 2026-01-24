using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Database;
using DG_TonKhoBTP_v02.Helper.Reuseable;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;

namespace DG_TonKhoBTP_v02.UI.NghiepVu.KeHoach
{
    public partial class BanHanhKH_SX : UserControl
    {
        private bool _suppressTextChanged = false;
        private readonly System.Windows.Forms.Timer _debounce = new System.Windows.Forms.Timer { Interval = 250 };

        private readonly BindingList<DSPhatHanhKHSX> _items = new BindingList<DSPhatHanhKHSX>();
        private readonly BindingSource _bs = new BindingSource();

        private Control _lastControl = null;

        public BanHanhKH_SX()
        {
            InitializeComponent();

            cbxTimTenSP.TextUpdate += AnyCombo_TextUpdate;

            _debounce.Tick += (s, e) =>
            {
                _debounce.Stop();
                if (_suppressTextChanged) return;

                TimSP(cbxTimTenSP.Text.Trim());
            };
        }


        private void TimSP(string t)
        {
            t = (t ?? "").Trim();
            if (t.Length == 0) { cbxTimTenSP.DroppedDown = false; return; }

            string sql = @"
            SELECT DISTINCT d.Ten
            FROM KeHoachSX k
            JOIN DanhSachMaSP d ON k.DanhSachMaSP_ID = d.id
            WHERE d.Ten LIKE @kw
              AND k.TinhTrangKH = 1
              AND k.TrangThaiSX = 0
            ORDER BY d.Ten;";

            DataTable dt = DatabaseHelper.GetData(sql, "%" + t + "%", "kw");

            if (dt.Rows.Count == 0)
            {
                _suppressTextChanged = true;
                try { cbxTimTenSP.DroppedDown = false; cbxTimTenSP.DataSource = null; }
                finally { _suppressTextChanged = false; }
                return;
            }

            var list = dt.AsEnumerable()
                .Select(r => new { Ten = r.Field<string>("Ten") })
                .ToList();

            _suppressTextChanged = true;
            try
            {
                string keepText = cbxTimTenSP.Text;

                cbxTimTenSP.BeginUpdate();
                cbxTimTenSP.DataSource = null;

                cbxTimTenSP.DisplayMember = "Ten";
                cbxTimTenSP.ValueMember = "Ten";
                cbxTimTenSP.DataSource = list;

                cbxTimTenSP.EndUpdate();

                cbxTimTenSP.Text = keepText;
                cbxTimTenSP.SelectionStart = cbxTimTenSP.Text.Length;
                cbxTimTenSP.SelectionLength = 0;
                cbxTimTenSP.DroppedDown = true;
            }
            finally { _suppressTextChanged = false; }
        }



        private void AnyCombo_TextUpdate(object sender, EventArgs e)
        {
            if (_suppressTextChanged) return;

            _lastControl = (Control)sender;
            _debounce.Stop();
            _debounce.Start();
        }

        private void cbxTimTenSP_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Commit nếu đang edit trong grid
            dtgDSKeHoach.EndEdit();
            _bs.EndEdit();

            // Lấy Ten user chọn
            string ten = (cbxTimTenSP.SelectedValue != null
                            ? cbxTimTenSP.SelectedValue.ToString()
                            : cbxTimTenSP.Text) ?? "";
            ten = ten.Trim();
            if (ten.Length == 0) return;

            string sql = @"
                SELECT
                  k.Lot   AS lot,
                  d.Ten   AS Ten,
                  k.GhiChu AS GhiChuKH
                FROM KeHoachSX k
                JOIN DanhSachMaSP d ON k.DanhSachMaSP_ID = d.id
                WHERE d.Ten = @ten COLLATE NOCASE
                  AND k.TinhTrangKH = 1
                  AND k.TrangThaiSX = 0
                ORDER BY k.id DESC;";

            DataTable dt = DatabaseHelper.GetData(sql, ten, "ten");
            if (dt == null || dt.Rows.Count == 0) return;

            HashSet<string> existing = new HashSet<string>(
                _items.Select(x => (x.Lot ?? "").Trim()),
                StringComparer.OrdinalIgnoreCase);

            int added = 0;

            foreach (DataRow row in dt.Rows)
            {
                string lot = (row["lot"] != null ? row["lot"].ToString() : "");
                lot = (lot ?? "").Trim();
                if (lot.Length == 0) continue;

                if (existing.Contains(lot)) continue;

                string tenDb = (row["ten"] != null ? row["ten"].ToString() : ten);
                tenDb = (tenDb ?? "").Trim();

                string ghiChu = (row["GhiChuKH"] != null ? row["GhiChuKH"].ToString() : "");
                ghiChu = (ghiChu ?? "").Trim();

                DSPhatHanhKHSX item = new DSPhatHanhKHSX();
                item.Lot = lot;
                item.Ten = tenDb;
                item.GhiChuKH = ghiChu;

                _items.Add(item);     // ✅ add vào BindingList -> grid tự cập nhật
                existing.Add(lot);
                added++;
            }

            if (added == 0)
                FrmWaiting.ShowGifAlert("Các Mã KH của sản phẩm này đã có trong danh sách.");

            // Reset combobox
            cbxTimTenSP.SelectedIndex = -1;
            cbxTimTenSP.Text = "";
            cbxTimTenSP.DroppedDown = false;
        }


        private void BanHanhKH_SX_Load(object sender, EventArgs e)
        {
            // Giữ nguyên độ rộng cột (không auto resize theo nội dung)
            dtgDSKeHoach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Cho phép xuống dòng trong cell
            dtgDSKeHoach.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dtgDSKeHoach.RowsDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            // Tự tăng chiều cao row theo nội dung
            dtgDSKeHoach.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // (tuỳ chọn) bật wrap cho header nếu tiêu đề cột dài
            dtgDSKeHoach.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dtgDSKeHoach.AutoResizeColumnHeadersHeight();

            dtgDSKeHoach.RowTemplate.MinimumHeight = 35;
            dtgDSKeHoach.RowTemplate.Height = 35;

            // ✅ BINDING
            dtgDSKeHoach.AutoGenerateColumns = false; // vì bạn đã tạo cột sẵn theo Name

            // Map cột -> property (Name cột: maKH, ten, ghiChu)
            if (dtgDSKeHoach.Columns.Contains("lot"))
                dtgDSKeHoach.Columns["lot"].DataPropertyName = nameof(DSPhatHanhKHSX.Lot);

            if (dtgDSKeHoach.Columns.Contains("ten"))
                dtgDSKeHoach.Columns["ten"].DataPropertyName = nameof(DSPhatHanhKHSX.Ten);

            if (dtgDSKeHoach.Columns.Contains("ghiChuKH"))
                dtgDSKeHoach.Columns["ghiChuKH"].DataPropertyName = nameof(DSPhatHanhKHSX.GhiChuKH);

            if (dtgDSKeHoach.Columns.Contains("GhiChuSX"))
                dtgDSKeHoach.Columns["GhiChuSX"].DataPropertyName = nameof(DSPhatHanhKHSX.GhiChuSX);

            // Nếu có các cột bool thì map thêm (chỉ map nếu tồn tại)
            if (dtgDSKeHoach.Columns.Contains("Rut"))
                dtgDSKeHoach.Columns["Rut"].DataPropertyName = nameof(DSPhatHanhKHSX.Rut);

            if (dtgDSKeHoach.Columns.Contains("Ben"))
                dtgDSKeHoach.Columns["Ben"].DataPropertyName = nameof(DSPhatHanhKHSX.Ben);

            if (dtgDSKeHoach.Columns.Contains("QB"))
                dtgDSKeHoach.Columns["QB"].DataPropertyName = nameof(DSPhatHanhKHSX.QB);

            if (dtgDSKeHoach.Columns.Contains("BocLot"))
                dtgDSKeHoach.Columns["BocLot"].DataPropertyName = nameof(DSPhatHanhKHSX.BocLot);

            if (dtgDSKeHoach.Columns.Contains("BocMach"))
                dtgDSKeHoach.Columns["BocMach"].DataPropertyName = nameof(DSPhatHanhKHSX.BocMach);

            if (dtgDSKeHoach.Columns.Contains("BocVo"))
                dtgDSKeHoach.Columns["BocVo"].DataPropertyName = nameof(DSPhatHanhKHSX.BocVo);

            // Gắn DataSource
            _bs.DataSource = _items;
            dtgDSKeHoach.DataSource = _bs;

            if (dtgDSKeHoach.Columns.Contains("maKH"))
                dtgDSKeHoach.Columns["maKH"].ReadOnly = true;

            if (dtgDSKeHoach.Columns.Contains("ten"))
                dtgDSKeHoach.Columns["ten"].ReadOnly = true;

            //if (dtgDSKeHoach.Columns.Contains("ghiChuKH"))
            //    dtgDSKeHoach.Columns["ghiChuKH"].ReadOnly = true;

            // Thêm nút xoá nếu chưa có
            const string deleteColName = "colDelete";
            if (!dtgDSKeHoach.Columns.Contains(deleteColName))
            {
                DataGridViewButtonColumn btnCol = new DataGridViewButtonColumn();
                btnCol.Name = deleteColName;
                btnCol.HeaderText = "Xoá";
                btnCol.Text = "Xoá";
                btnCol.UseColumnTextForButtonValue = true;
                btnCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                btnCol.Width = 60;

                dtgDSKeHoach.Columns.Add(btnCol);
            }

            // ✅ Gắn sự kiện click nút
            dtgDSKeHoach.CellContentClick -= dtgDSKeHoach_CellContentClick;
            dtgDSKeHoach.CellContentClick += dtgDSKeHoach_CellContentClick;
        }



        private void dtgDSKeHoach_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // click header

            if (dtgDSKeHoach.Columns[e.ColumnIndex].Name == "colDelete")
            {
                if (!dtgDSKeHoach.Rows[e.RowIndex].IsNewRow)
                    dtgDSKeHoach.Rows.RemoveAt(e.RowIndex);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            dtgDSKeHoach.Rows.Clear();
            dtgDSKeHoach.ClearSelection();
        }

        private void btnInKHSX_Click(object sender, EventArgs e)
        {
            List<DSPhatHanhKHSX> danhSach = _items.ToList();

            float[] colMm = { 70f, 28f, 55f, 55f };

            KhsxPrintService.PrintDsPhatHanh_ByFlags(
                danhSach: danhSach,
                colWidthsMm: colMm,
                printerName: null,   // hoặc "Tên máy in" / "Microsoft Print to PDF"
                paperName: "A5",
                landscape: true,
                nguoiLap: "Người lập",
                tenNguoiLap: ""
            );
        }

    }
}
