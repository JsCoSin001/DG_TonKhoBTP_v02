  using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Helper
{
    public static class Helper
    {
        public static string _connStr;
        public static string GetShiftValue()
        {
            int hour = DateTime.Now.Hour;

            if (hour >= 6 && hour < 14)
                return "1";

            if (hour >= 14 && hour < 22)
                return "2";

            return "3";

        }
        public static string TaoSQL_LayDLTTThanhPham()
        {
            // KHÔNG đặt dấu ; ở cuối vì còn nối UNION ALL ở hàm sau
            return @"
                SELECT
                    t.KhoiLuongSau AS KlBatDau,
                    t.ChieuDaiSau  AS CDBatDau,
                    t.id           AS id,
                    t.MaBin        AS BinNVL
                FROM TTThanhPham AS t
                JOIN DanhSachMaSP AS d
                    ON d.id = t.DanhSachSP_ID
                WHERE
                    (
                        (d.DonVi = 0 AND t.KhoiLuongSau <> 0)
                        OR
                        (d.DonVi = 1 AND t.ChieuDaiSau  <> 0)
                    )
                    AND (
                        @para IS NULL OR TRIM(@para) = ''
                        OR t.MaBin LIKE '%' || @para || '%' COLLATE NOCASE
                    )
            ";
        }

        public static string TaoSQL_LayDLNVL_TTThanhPham()
        {
            string baseQuery = TaoSQL_LayDLTTThanhPham(); // KHÔNG có ; ở cuối

            return baseQuery + @"

                UNION ALL

                SELECT
                    -1   AS KlBatDau,
                    -1   AS CDBatDau,
                    d.id AS id,
                    d.ten AS BinNVL
                FROM DanhSachMaSP AS d
                WHERE
                    d.Ma LIKE 'NVL.%' COLLATE NOCASE
                    AND (
                        @para IS NULL OR TRIM(@para) = ''
                        OR d.Ten LIKE '%' || @para || '%' COLLATE NOCASE
                        OR d.Ma  LIKE '%' || @para || '%' COLLATE NOCASE
                    )
            ";
            
        }

        public static string TaoKhoangTrong(int tongKhoangTrong, string noiDung)
        {
            return new string(' ', tongKhoangTrong - noiDung.Length);

        }
        public static string GetNgayHienTai()
        {
            DateTime now = DateTime.Now;
            DateTime ngayHienTai = (now.TimeOfDay < new TimeSpan(6, 0, 0))
                ? DateTime.Today.AddDays(-1)
                : DateTime.Today;

            return ngayHienTai.ToString("yyyy-MM-dd");
        }

        public static string TaoSqL_LayThongTinChung()
        {
            return @"
                SELECT
                  ttp.id AS STT,
                  tclv.Ngay, tclv.Ca,tclv.May,
                  ttp.MaBin,ds.Ten AS TEN_SP, tclv.NguoiLam,
                  ttp.KhoiLuongTruoc, ttp.KhoiLuongSau,
                  ttp.ChieuDaiTruoc, ttp.ChieuDaiSau,
                  ttp.Phe, ttp.GhiChu, ";
        }

        public static string TaoSQL_TaoKetNoiCacBang()
        {
            return @"
                FROM TTThanhPham ttp
                JOIN ThongTinCaLamViec tclv ON tclv.id = ttp.ThongTinCaLamViec_ID
                JOIN DanhSachMaSP ds        ON ds.id   = ttp.DanhSachSP_ID
                LEFT JOIN CD_BocVo    cbv ON cbv.TTThanhPham_ID    = ttp.id
                LEFT JOIN CD_BocLot   cbl ON cbl.TTThanhPham_ID    = ttp.id
                LEFT JOIN CD_BocMach  cbm ON cbm.TTThanhPham_ID    = ttp.id
                LEFT JOIN CD_KeoRut   ckr ON ckr.TTThanhPham_ID    = ttp.id
                LEFT JOIN CD_BenRuot  cbr ON cbr.TTThanhPham_ID    = ttp.id
                LEFT JOIN CD_GhepLoiQB cgl ON cgl.TTThanhPham_ID   = ttp.id
                LEFT JOIN CaiDatCDBoc cdb ON cdb.TTThanhPham_ID   = ttp.id
                LEFT JOIN TTNVL       nvl ON nvl.TTThanhPham_ID    = ttp.id";
        }

        public static string TaoSQL_LayChiTietCongDoan(int id)
        {
            string sqlLayChiTietCD = "";

            switch (id)
            {
                case 1: // Kéo rút
                    sqlLayChiTietCD = "ckr.DKTrucX, ckr.DKTrucY, ckr.NgoaiQuan AS KeoRut_NgoaiQuan, ckr.TocDo, ckr.DienApU, ckr.DongDienU,";
                    break;
                case 2: // Bện ruột
                    sqlLayChiTietCD = "cbr.DKSoi, cbr.SoSoi, cbr.ChieuXoan AS BenRuot_ChieuXoan, cbr.BuocBen,";
                    break;
                case 3: // Ghép lõi - Quấn băng
                    sqlLayChiTietCD = "cgl.BuocXoan, cgl.ChieuXoan, cgl.GoiCachMep, cgl.DKBTP,";
                    break;
                case 4: // Bọc mạch
                    sqlLayChiTietCD = "cbm.NgoaiQuan AS BocMach_NgoaiQuan, cbm.LanDanhThung, cbm.SoMet,";
                    break;
                case 5: // Bóc lót
                    sqlLayChiTietCD = "cbl.DoDayTBLot,";
                    break;
                case 6: // Bóc vỏ
                    sqlLayChiTietCD = "cbv.DayVoTB, cbv.InAn, ";
                    break;
                default:
                    break;
            }
            return sqlLayChiTietCD;
        }

        public static string LOTGenerated(ComboBox may, NumericUpDown maHT, ComboBox sttCongDoan, NumericUpDown sttBin, NumericUpDown soBin)
        {
            string lot = "";

            // Kiểm tra maHT có đủ 6 chữ số
            int maHTValue = (int)maHT.Value;
            if (maHTValue < 100000 || maHTValue > 999999)
                return lot;

            // Kiểm tra ComboBox 'may'
            if (may.Text == null ||
                string.IsNullOrWhiteSpace(may.Text) ||
                may.Text == "0")
                return lot;

            // Kiểm tra sttCongDoan
            if (sttCongDoan.SelectedItem == null ||
                string.IsNullOrWhiteSpace(sttCongDoan.Text) ||
                sttCongDoan.Text == "0")
                return lot;

            // Kiểm tra sttBin và soBin
            if (sttBin.Value == 0)
                return lot;

            string sttBinT = sttBin.Value < 10 ? "0" + sttBin.Text : sttBin.Text;
            string soBinT = soBin.Value < 10 ? "0" + soBin.Text : soBin.Text;

            // Tạo mã LOT: may-maHT-sttCongDoan-sttBin-soBin
            lot = $"{may.Text}-{maHTValue}/{sttCongDoan.Text}-{sttBinT}-{soBinT}";

            return lot;
        }

        // Có thể đặt trong UC_SubmitForm hoặc class Helper chung
        public static T FindControlRecursive<T>(Control root) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t)
                    return t;

                if (c.HasChildren)
                {
                    var child = FindControlRecursive<T>(c);
                    if (child != null)
                        return child;
                }
            }
            return null;
        }

        public static void MapRowToObject<T>(DataGridViewRow row, T target)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var p in props)
            {
                if (!row.DataGridView.Columns.Contains(p.Name))
                    continue;

                var raw = row.Cells[p.Name]?.Value;

                try
                {
                    object value = null;

                    if (p.PropertyType == typeof(string))
                    {
                        value = raw?.ToString() ?? string.Empty;
                    }
                    else if (IsNumeric(p.PropertyType))
                    {
                        // Ô trống => 0
                        var s = raw?.ToString();
                        if (string.IsNullOrWhiteSpace(s))
                        {
                            value = ConvertToNumericDefaultZero(p.PropertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(s, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
                        }
                    }
                    else
                    {
                        // Kiểu khác (int?, double?, …)
                        var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        if (raw == null || string.IsNullOrWhiteSpace(raw.ToString()))
                        {
                            value = underlying.IsValueType ? Activator.CreateInstance(underlying) : null;
                        }
                        else
                        {
                            value = Convert.ChangeType(raw, underlying);
                        }
                    }

                    p.SetValue(target, value);
                }
                catch
                {
                    // Nếu chuyển kiểu lỗi -> gán 0 cho numeric, "" cho string
                    if (p.PropertyType == typeof(string))
                        p.SetValue(target, string.Empty);
                    else if (IsNumeric(p.PropertyType))
                        p.SetValue(target, ConvertToNumericDefaultZero(p.PropertyType));
                }
            }
        }

        private static bool IsNumeric(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
                   t == typeof(double) || t == typeof(float) || t == typeof(decimal);
        }

        private static object ConvertToNumericDefaultZero(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int)) return 0;
            if (t == typeof(long)) return 0L;
            if (t == typeof(short)) return (short)0;
            if (t == typeof(double)) return 0.0d;
            if (t == typeof(float)) return 0.0f;
            if (t == typeof(decimal)) return 0.0m;
            return 0;
        }
    }


}
