using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    /// <summary>
    /// Một form và một DataGridView dùng chung. Mỗi lần mở chỉ hiển thị
    /// nguyên liệu HOẶC ca làm việc (không có hai grid / tab).
    /// </summary>
    public sealed class Frm_MonthyReportDetail : Form
    {
        private readonly DataGridView detailGrid;
        private readonly Form _parentForm;

        public Frm_MonthyReportDetail(Form parentForm)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            Text = "Chi tiết báo cáo tháng";
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            MinimumSize = new Size(520, 350);
            Load += (s, e) => SizeAndCenterWithinOwner();
            Padding = new Padding(8);

            detailGrid = new DataGridView
            {
                Name = "detailGrid",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 45,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                BackgroundColor = Color.White
            };
            detailGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F);
            detailGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            detailGrid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            detailGrid.RowTemplate.Height = 32;
            Controls.Add(detailGrid);
        }

        // Form chi tiết chiếm 70% bề rộng của form gọi, căn giữa cả hai chiều.
        // Dùng vùng làm việc màn hình để form không bị khuất ngoài màn hình.
        private void SizeAndCenterWithinOwner()
        {
            Rectangle bounds = _parentForm.Bounds;
            Rectangle workingArea = Screen.FromRectangle(bounds).WorkingArea;
            int width = Math.Min(workingArea.Width,
                Math.Max(MinimumSize.Width, (int)Math.Round(bounds.Width * 0.7)));
            int height = Math.Min(workingArea.Height,
                Math.Max(MinimumSize.Height, (int)Math.Round(bounds.Height * 0.8)));
            Size = new Size(width, height);

            int x = bounds.Left + (bounds.Width - Width) / 2;
            int y = bounds.Top + (bounds.Height - Height) / 2;
            Location = new Point(
                Math.Max(workingArea.Left, Math.Min(x, workingArea.Right - Width)),
                Math.Max(workingArea.Top, Math.Min(y, workingArea.Bottom - Height)));
        }

        private void ResetGrid()
        {
            detailGrid.DataSource = null;
            detailGrid.Columns.Clear();
        }

        // Chiều rộng các cột đặt thủ công tại LoadMaterials() và LoadShift().
        // Chi tiết NVL không có TTThanhPham.MaBin/TenSanPham: cặp tương ứng
        // cần vừa dữ liệu là BinNVL (bin nguyên liệu) và TenNVL (tên nguyên liệu).
        private void AddColumn(DataTable table, string key, string title, Type type, bool number = false, int width = 120)
        {
            table.Columns.Add(key, type);
            var column = new DataGridViewTextBoxColumn
            {
                Name = key,
                DataPropertyName = key,
                HeaderText = title,
                SortMode = DataGridViewColumnSortMode.Automatic,
                AutoSizeMode = key == "BinNVL" || key == "TenNVL"
                    ? DataGridViewAutoSizeColumnMode.AllCells
                    : DataGridViewAutoSizeColumnMode.None,
                Width = width
            };
            if (number) column.DefaultCellStyle.Format = "#,##0.##";
            detailGrid.Columns.Add(column);
        }

        /// <summary>Chỉ 10 cột chung cộng cột kỹ thuật tương ứng công đoạn.</summary>
        public void LoadMaterials(IReadOnlyList<MonthyReport_MaterialModel> rows, CongDoan congDoan,
                                  long ttThanhPhamId, string maBin)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (congDoan == null) throw new ArgumentNullException(nameof(congDoan));
            ResetGrid();
            Text = $"Chi tiết nguyên liệu — STT: {ttThanhPhamId} — Bin: {maBin}";
            var table = new DataTable("Materials");
            AddColumn(table, "STT", "STT", typeof(int), width: 65);
            AddColumn(table, "BinNVL", "Bin nguyên liệu", typeof(string));
            AddColumn(table, "MaNVL", "Mã nguyên liệu", typeof(string), width: 145);
            AddColumn(table, "TenNVL", "Tên nguyên liệu", typeof(string));
            AddColumn(table, "DonVi", "ĐVT", typeof(string), width: 75);
            AddColumn(table, "KlBatDau", "KL bắt đầu", typeof(double), true, width: 125);
            AddColumn(table, "KlConLai", "KL còn lại", typeof(double), true, width: 120);
            AddColumn(table, "CdBatDau", "CD bắt đầu", typeof(double), true, width: 125);
            AddColumn(table, "CdConLai", "CD còn lại", typeof(double), true, width: 120);
            AddColumn(table, "QC", "QC", typeof(string), width: 75);

            string[] technical = GetTechnicalColumns(congDoan).ToArray();
            foreach (string key in technical)
            {
                AddColumn(table, key, TechnicalTitle(key), typeof(double), true, 135);
            }

            int position = 0;
            foreach (var row in rows)
            {
                var record = table.NewRow();
                record["STT"] = ++position;
                record["BinNVL"] = ValueForDisplay(row.BinNVL);
                record["MaNVL"] = ValueForDisplay(row.MaNVL);
                record["TenNVL"] = ValueForDisplay(row.TenNVL);
                record["DonVi"] = ValueForDisplay(row.DonVi);
                record["KlBatDau"] = ValueForDisplay(row.KlBatDau);
                record["KlConLai"] = ValueForDisplay(row.KlConLai);
                record["CdBatDau"] = ValueForDisplay(row.CdBatDau);
                record["CdConLai"] = ValueForDisplay(row.CdConLai);
                record["QC"] = ValueForDisplay(row.QC);
                foreach (string key in technical)
                {
                    record[key] = ValueForDisplay(TechnicalValue(row, key));
                }
                table.Rows.Add(record);
            }
            detailGrid.DataSource = table.DefaultView;
        }

        public void LoadShift(IReadOnlyList<MonthyReport_ShiftModel> rows, long ttThanhPhamId, string maBin)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            ResetGrid();
            Text = $"Chi tiết ca làm việc — STT: {ttThanhPhamId} — Bin: {maBin}";
            var table = new DataTable("Shifts");
            AddColumn(table, "May", "Máy", typeof(string), width: 85);
            AddColumn(table, "Ca", "Ca bắt đầu", typeof(string), width: 115);
            AddColumn(table, "NguoiLam", "Người làm", typeof(string), width: 150);
            AddColumn(table, "NgayBatDau", "Ngày bắt đầu", typeof(string), width: 135);
            AddColumn(table, "GioBatDau", "Giờ bắt đầu", typeof(string), width: 115);
            AddColumn(table, "NgayKetThuc", "Ngày kết thúc", typeof(string), width: 135);
            AddColumn(table, "GioKetThuc", "Giờ kết thúc", typeof(string), width: 115);
            AddColumn(table, "CaKetThuc", "Ca kết thúc", typeof(string), width: 120);
            AddColumn(table, "NguoiKetThuc", "Người kết thúc", typeof(string), width: 160);
            AddColumn(table, "ToTruong", "Tổ trưởng", typeof(string), width: 135);
            AddColumn(table, "QuanDoc", "Quản đốc", typeof(string), width: 135);

            foreach (var row in rows)
            {
                var record = table.NewRow();
                record["May"] = ValueForDisplay(row.May);
                record["Ca"] = ValueForDisplay(row.Ca);
                record["NguoiLam"] = ValueForDisplay(row.NguoiLam);
                record["NgayBatDau"] = ValueForDisplay(row.NgayBatDau);
                record["GioBatDau"] = ValueForDisplay(row.GioBatDau);
                record["NgayKetThuc"] = ValueForDisplay(row.NgayKetThuc);
                record["GioKetThuc"] = ValueForDisplay(row.GioKetThuc);
                record["CaKetThuc"] = ValueForDisplay(row.CaKetThuc);
                record["NguoiKetThuc"] = ValueForDisplay(row.NguoiKetThuc);
                record["ToTruong"] = ValueForDisplay(row.ToTruong);
                record["QuanDoc"] = ValueForDisplay(row.QuanDoc);
                table.Rows.Add(record);
            }
            detailGrid.DataSource = table.DefaultView;
        }

        internal static IReadOnlyList<string> GetTechnicalColumns(CongDoan congDoan)
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                "DuongKinhSoiDong", "SoSoi", "KetCauLoi", "DuongKinhSoiMach"
            };
            var selected = new List<string>();
            foreach (var column in congDoan.Columns ?? new List<ColumnDefinition>())
            {
                if (allowed.Contains(column.Name) && !selected.Contains(column.Name))
                    selected.Add(column.Name);
            }
            if (congDoan.Id == 6 || congDoan.Id == 7 || congDoan.Id == 8)
            {
                if (!selected.Contains("BanRongBang")) selected.Add("BanRongBang");
                if (!selected.Contains("DoDayBang")) selected.Add("DoDayBang");
            }
            return selected;
        }

        private static string TechnicalTitle(string key)
        {
            switch (key)
            {
                case "DuongKinhSoiDong": return "ĐK sợi đồng";
                case "SoSoi": return "Số sợi";
                case "KetCauLoi": return "Kết cấu lõi";
                case "DuongKinhSoiMach": return "ĐK sợi mạch";
                case "BanRongBang": return "Bản rộng băng";
                case "DoDayBang": return "Độ dày băng";
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }

        private static double? TechnicalValue(MonthyReport_MaterialModel row, string key)
        {
            switch (key)
            {
                case "DuongKinhSoiDong": return row.DuongKinhSoiDong;
                case "SoSoi": return row.SoSoi;
                case "KetCauLoi": return row.KetCauLoi;
                case "DuongKinhSoiMach": return row.DuongKinhSoiMach;
                case "BanRongBang": return row.BanRongBang;
                case "DoDayBang": return row.DoDayBang;
                default: throw new ArgumentOutOfRangeException(nameof(key));
            }
        }

        // Định dạng thiếu dữ liệu chỉ tại lớp giao diện. Không sửa Model/SQLite.
        internal static object ValueForDisplay(object? value)
        {
            if (value == null || value == DBNull.Value) return DBNull.Value;
            if (value is string text && text == "-1") return DBNull.Value;
            if (value is double d && d == -1) return DBNull.Value;
            if (value is float f && f == -1) return DBNull.Value;
            if (value is decimal dec && dec == -1) return DBNull.Value;
            if (value is int i && i == -1) return DBNull.Value;
            if (value is long l && l == -1) return DBNull.Value;
            return value;
        }
    }
}
