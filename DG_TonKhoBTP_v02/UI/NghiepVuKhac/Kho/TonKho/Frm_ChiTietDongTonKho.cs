using DG_TonKhoBTP_v02.Models.Kho.TonKho;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.UI
{
    internal sealed class Frm_ChiTietDongTonKho : Form
    {
        private readonly BCTonKho_ExcelDong _dong;
        private readonly BCTonKho_ChiTietDong _chiTiet;
        private readonly DataGridView? _nguon;
        private readonly DataGridView _lichSu = TaoBang("grvLichSuCat",
            "Thời gian", "Mã cuộn", "Loại lần cắt", "Người thực hiện", "Mã kế hoạch", "Chiều dài cắt",
            "Trước cắt", "Đoạn cắt", "Sau cắt");
        private readonly Label _trangThaiLichSu = new Label
        {
            Name = "lblLichSu", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(4)
        };

        internal Frm_ChiTietDongTonKho(BCTonKho_ExcelDong dong)
        {
            _dong = dong;
            _chiTiet = dong.ChiTiet ?? throw new ArgumentException("Dòng báo cáo thiếu dữ liệu chi tiết.", nameof(dong));
            bool cuonChan = dong.Loai == "Cuộn";
            var lotGop = _chiTiet.LotGop;

            Text = "Chi tiết dòng báo cáo tồn kho";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1110, 690);
            MinimumSize = new Size(760, 530);
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(10), RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, lotGop.Count > 1 ? 134 : 0));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(layout);
            layout.Controls.Add(new TextBox
            {
                Name = "txtTongHop", AccessibleName = "Số liệu trên dòng báo cáo", Dock = DockStyle.Fill,
                Multiline = true, ReadOnly = true, BackColor = SystemColors.Window,
                ScrollBars = ScrollBars.Vertical, Text = TaoTongHop(dong)
            }, 0, 0);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            if (cuonChan)
            {
                _nguon = TaoBang("grvNguon", "Nguồn nhập", "LOT", "Cuộn nhập", "Đã lấy nguyên",
                    "Đã chuyển lẻ", "Cuộn còn lại", "Cuộn góp tồn", "Chiều dài góp tồn");
                var tabNguon = new TabPage("Nguồn tồn");
                tabNguon.Controls.Add(_nguon);
                tabs.TabPages.Add(tabNguon);
            }
            var tabLichSu = new TabPage("Lịch sử cắt");
            tabLichSu.Controls.Add(_lichSu);
            tabLichSu.Controls.Add(_trangThaiLichSu);
            tabs.TabPages.Add(tabLichSu);
            var tabDatTruoc = new TabPage("Đặt trước theo kế hoạch");
            tabDatTruoc.Controls.Add(TaoBangDatTruoc(cuonChan));
            tabs.TabPages.Add(tabDatTruoc);
            layout.Controls.Add(tabs, 0, 2);

            if (lotGop.Count > 1)
            {
                var danhSach = TaoBang("grvLotGop", "LOT góp tồn", "Cuộn còn lại", "Chiều dài góp tồn");
                foreach (var lot in lotGop)
                    danhSach.Rows.Add(lot.MaBin, So(lot.SoCuon), So(lot.SoMet));
                danhSach.CurrentCellChanged += (sender, args) =>
                    HienNguonVaLichSu(danhSach.CurrentCell?.OwningRow.Cells[0].Value as string, true);
                var group = new GroupBox { Text = "Chọn LOT góp tồn để xem chi tiết", Dock = DockStyle.Fill };
                group.Controls.Add(danhSach);
                layout.Controls.Add(group, 0, 1);
                Shown += (sender, args) => { danhSach.CurrentCell = null; danhSach.ClearSelection(); };
                HienNguonVaLichSu(null, true);
            }
            else
            {
                HienNguonVaLichSu(lotGop.FirstOrDefault()?.MaBin, false);
                tabs.SelectedTab = cuonChan && _chiTiet.Nguon.Count == 0 ? tabDatTruoc : tabLichSu;
            }

            var close = new Button { Text = "Đóng", DialogResult = DialogResult.Cancel, AutoSize = true };
            CancelButton = close;
            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            footer.Controls.Add(close);
            layout.Controls.Add(footer, 0, 3);
            ActiveControl = close;
        }

        private static string TaoTongHop(BCTonKho_ExcelDong dong)
        {
            string donVi = dong.DonVi;
            string tongHop = $"Sản phẩm  {dong.TenSanPham} ({dong.MaSanPham})    Đơn vị  {donVi}" + Environment.NewLine
                + $"Loại  {dong.Loai}    LOT  {dong.LotHienThiBang}" + Environment.NewLine
                + $"Số cuộn  {So(dong.SoCuon)}    Chiều dài mỗi cuộn  {So(dong.ChieuDaiMoiCuon)} {donVi}" + Environment.NewLine
                + $"Tồn kho thực  {So(dong.SoCuon)} × {So(dong.ChieuDaiMoiCuon)} = {So(dong.ChieuDaiTonThucTe)} {donVi}"
                + $"    Đặt trước  {So(dong.ChieuDaiTonDatTruoc)} {donVi}"
                + $"    Tồn khả dụng  {So(dong.ChieuDaiTonThucTe)} − {So(dong.ChieuDaiTonDatTruoc)} = {So(dong.ChieuDaiTonKhaDung)} {donVi}";
            if (dong.Loai == "Lẻ")
            {
                string soDau = dong.SoDau?.ToString("N0") ?? string.Empty;
                string soCuoi = dong.SoCuoi?.ToString("N0") ?? string.Empty;
                tongHop += Environment.NewLine + $"Số đầu  {soDau}    Số cuối  {soCuoi}";
                if (dong.SoDau.HasValue && dong.SoCuoi.HasValue)
                    tongHop += $"    Chiều dài còn lại  |{soCuoi} − {soDau}| = {So(dong.ChieuDaiMoiCuon)} {donVi}";
            }
            if (dong.LaBatThuong)
                tongHop += Environment.NewLine + "Cảnh báo" + Environment.NewLine + dong.CanhBao;
            return tongHop;
        }

        private void HienNguonVaLichSu(string? maBin, bool nhieuLot)
        {
            bool doiChonLot = nhieuLot && maBin == null;
            var nguonDaChon = _chiTiet.Nguon.Where(x => maBin == null
                ? !doiChonLot
                : StringComparer.OrdinalIgnoreCase.Equals(x.MaBin, maBin)).ToList();
            if (_nguon != null)
            {
                _nguon.Rows.Clear();
                var thieuMa = _chiTiet.Nguon.Where(x =>
                    (maBin != null || doiChonLot) && x.MaBin.Length == 0 && x.SoCuonGop > 0);
                foreach (var nguon in nguonDaChon.Concat(thieuMa))
                {
                    _nguon.Rows.Add($"#{nguon.TTCuonDayId}", nguon.MaBin.Length == 0 ? "Thiếu mã LOT" : nguon.MaBin,
                        So(nguon.SoCuonNhap), So(nguon.SoCuonDaLay), So(nguon.SoCuonChuyenLe),
                        So(nguon.SoCuonCon), So(nguon.SoCuonGop), So((decimal)nguon.SoCuonGop * _dong.ChieuDaiMoiCuon));
                }
            }

            _lichSu.Rows.Clear();
            var ids = nguonDaChon.Select(x => x.TTCuonDayId).ToHashSet();
            if (!doiChonLot)
            {
                foreach (var cut in _chiTiet.LichSuCat.Where(x =>
                    _dong.Loai == "Lẻ" || (x.TTCuonDayId.HasValue && ids.Contains(x.TTCuonDayId.Value))))
                {
                    _lichSu.Rows.Add(cut.ThoiGian, cut.MaCuon,
                        cut.LoaiNguon == "CUON_CHAN" ? "Chuyển từ cuộn chẵn" : "Cắt cuộn lẻ",
                        cut.NguoiThucHien, cut.MaKeHoach, SoLichSu(cut.ChieuDaiCat),
                        Moc(cut.SoDauTruoc, cut.SoCuoiTruoc), Moc(cut.SoDauCat, cut.SoCuoiCat), Moc(cut.SoDauSau, cut.SoCuoiSau));
                }
            }
            _trangThaiLichSu.Text = doiChonLot ? "Chọn LOT để xem lịch sử cắt"
                : _lichSu.Rows.Count == 0 ? "Chưa có lịch sử cắt" : string.Empty;
            _trangThaiLichSu.Visible = _trangThaiLichSu.Text.Length > 0;
        }

        private DataGridView TaoBangDatTruoc(bool cuonChan)
        {
            var bang = cuonChan
                ? TaoBang("grvDatTruoc", "Mã kế hoạch", "Cuộn còn phải lấy", "Cuộn dành để cắt", "Chiều dài đặt trước")
                : TaoBang("grvDatTruoc", "Mã kế hoạch", "Chiều dài còn phải cắt");
            foreach (var keHoach in _chiTiet.DatTruoc.OrderBy(x => x.MaKeHoach, StringComparer.CurrentCultureIgnoreCase))
            {
                string ma = string.IsNullOrWhiteSpace(keHoach.MaKeHoach) ? $"#{keHoach.KeHoachId}" : keHoach.MaKeHoach;
                if (cuonChan)
                    bang.Rows.Add(ma, So(keHoach.SoCuonConPhaiLay), So(keHoach.SoCuonDanhDeCat), So(keHoach.SoMet));
                else
                    bang.Rows.Add(ma, So(keHoach.SoMet));
            }
            return bang;
        }

        private static string So(decimal giaTri) => giaTri.ToString("N0");
        private static string SoLichSu(long? giaTri) => giaTri?.ToString("N0") ?? "—";
        private static string Moc(long? dau, long? cuoi) => $"{SoLichSu(dau)} – {SoLichSu(cuoi)}";

        private static DataGridView TaoBang(string ten, params string[] tieuDe)
        {
            var bang = new DataGridView
            {
                Name = ten, Dock = DockStyle.Fill, ReadOnly = true, RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
            };
            for (int i = 0; i < tieuDe.Length; i++)
                bang.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = $"col{i}", HeaderText = tieuDe[i], SortMode = DataGridViewColumnSortMode.NotSortable
                });
            return bang;
        }
    }
}
