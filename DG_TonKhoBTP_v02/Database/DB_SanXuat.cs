// ============================================================
// DB_SanXuat.cs
// Bảng liên quan: TTThanhPham, ThongTinCaLamViec, TTNVL, CaiDatCDBoc,
//                 CD_BenRuot, CD_KeoRut, CD_GhepLoiQB, CD_BocMach, CD_BocLot, CD_BocVo
//                 LichSuSuaDoiThongTin, DanhSachBin
// Chức năng: Lưu / Cập nhật sản phẩm theo công đoạn, Lịch sử sửa đổi, In nhãn
// ============================================================

using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreHelper = DG_TonKhoBTP_v02.Helper.Helper;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DB_SanXuat
    {
        // ── Lưu sản phẩm mới ─────────────────────────────────────────────────

        public static bool SaveDataSanPham(ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (tp == null) { errorMsg = "Thiếu thông tin thành phẩm."; return false; }
            if (chiTietCD == null || chiTietCD.Count == 0 || chiTietCD[0] == null)
            { errorMsg = "Thiếu chi tiết công đoạn."; return false; }

            long idCaiDatCDBoc = 0;
            using var conn = DB_Base.OpenConnection();
            SQLiteTransaction tx = null;
            try
            {
                tx = conn.BeginTransaction();
                long tpId = InsertTTThanhPham(conn, tx, tp, nvl);
                InsertThongTinCaLamViec(conn, tx, caLam, tpId);
                InsertTTNVL(conn, tx, tpId, nvl);
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

                var congDoan = chiTietCD[0];
                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    CaiDatCDBoc caiDat = (chiTietCD.Count > 1) ? chiTietCD[1] as CaiDatCDBoc : null;
                    if (caiDat != null) idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, caiDat);
                }

                switch (congDoan)
                {
                    case CD_KeoRut keo: InsertCDKeoRut(conn, tx, tpId, keo); break;
                    case CD_BenRuot ben: InsertCDBenRuot(conn, tx, tpId, ben); break;
                    case CD_GhepLoiQB qb: InsertCDGhepLoiQB(conn, tx, tpId, qb); break;
                    case CD_BocLot bocLot: InsertCDBocLot(conn, tx, idCaiDatCDBoc, bocLot); break;
                    case CD_BocMach mach: InsertCDBocMach(conn, tx, idCaiDatCDBoc, mach); break;
                    case CD_BocVo vo: InsertCDBocVo(conn, tx, idCaiDatCDBoc, vo); break;
                    default: throw new ArgumentException("Công đoạn không hợp lệ.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                errorMsg = CoreHelper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
        }

        public static bool SaveTachBin(ThongTinCaLamViec caLam, List<TTThanhPham> list_tp, List<TTNVL> nvl, out string errorMsg)
        {
            errorMsg = string.Empty;
            using var conn = DB_Base.OpenConnection();
            SQLiteTransaction tx = null;
            try
            {
                tx = conn.BeginTransaction();
                foreach (TTThanhPham tp in list_tp)
                {
                    long tpId = InsertTTThanhPham(conn, tx, tp, nvl);
                    InsertThongTinCaLamViec(conn, tx, caLam, tpId);
                    InsertTTNVL(conn, tx, tpId, nvl);
                    var t = new List<TTNVL> { new TTNVL { BinNVL = nvl[0].BinNVL, KlConLai = 0, CdConLai = 0, QC = tp.QC } };
                    UpdateKL_CD_TTThanhPham(conn, tx, t, tpId);
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                errorMsg = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }
        }

        // ── Cập nhật sản phẩm ────────────────────────────────────────────────

        public static bool UpdateDataSanPham(int tpId, ThongTinCaLamViec caLam, TTThanhPham tp,
            List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            errorMsg = string.Empty;
            using var conn = DB_Base.OpenConnection();
            SQLiteTransaction tx = null;
            CaiDatCDBoc caiDat = (CaiDatCDBoc)chiTietCD[1];
            try
            {
                tx = conn.BeginTransaction();
                BackupThongTinTruocKhiSua(conn, tx, tpId, tp, caLam.NguoiLam);
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);
                UpdateTTThanhPham(conn, tx, tp, tpId, nvl);
                RestoreFromNVL(conn, tx, tpId);
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);
                Del_InsertTTNVL(conn, tx, tpId, nvl);
                if (caiDat != null) UpdateCaiDatCDBoc(conn, tx, tpId, caiDat);

                switch (chiTietCD[0])
                {
                    case CD_BenRuot ben: UpdateCDBenRuot(conn, tx, tpId, ben); break;
                    case CD_KeoRut keo: UpdateCDKeoRut(conn, tx, tpId, keo); break;
                    case CD_GhepLoiQB qb: UpdateCDGhepLoiQB(conn, tx, tpId, qb); break;
                    case CD_BocMach mach: UpdateCDBocMach(conn, tx, tpId, mach); break;
                    case CD_BocLot lot: UpdateCDBocLot(conn, tx, tpId, lot); break;
                    case CD_BocVo vo: UpdateCDBocVo(conn, tx, tpId, vo); break;
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                errorMsg = CoreHelper.ShowErrorDatabase(ex);
                return false;
            }
        }

        public static bool UpdateNguoiKiemTra(List<int> listStt, string nguoiKT)
        {
            if (listStt == null || listStt.Count == 0) return false;
            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = conn.CreateCommand();
                var paramNames = new List<string>();
                int i = 0;
                foreach (int stt in listStt)
                {
                    string p = "@p" + i;
                    paramNames.Add(p);
                    cmd.Parameters.AddWithValue(p, stt);
                    i++;
                }
                cmd.Parameters.AddWithValue("@nguoiKT", nguoiKT);
                cmd.CommandText = $"UPDATE ThongTinCaLamViec SET ToTruong = @nguoiKT WHERE TTThanhPham_id IN ({string.Join(",", paramNames)});";
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public static string UpdateKLConLai_BanTran(BanTran bt)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @KhoiLuongSau,
                    KLBanTran = COALESCE(KLBanTran, 0) + @KhoiLuongBanTran
                WHERE MaBin = @MaBin;";
            try
            {
                using var conn = DB_Base.OpenConnection();
                using var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@KhoiLuongSau", bt.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@KhoiLuongBanTran", bt.KhoiLuongBanTran);
                cmd.Parameters.AddWithValue("@MaBin", bt.MaBin);
                return cmd.ExecuteNonQuery() > 0 ? "" : $"Không tìm thấy bản ghi với MaBin = {bt.MaBin}";
            }
            catch (Exception ex) { return CoreHelper.ShowErrorDatabase(ex, bt.MaBin); }
        }

        // ── Truy xuất dữ liệu SX ─────────────────────────────────────────────

        /// <summary>Bảng: TTNVL — tìm kiếm lot NVL</summary>
        public static Task<DataTable> SearchLotSXAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                const string sql = @"
                    SELECT DISTINCT BinNVL FROM TTNVL
                    WHERE IFNULL(BinNVL, '') <> '' AND BinNVL LIKE '%' || @key || '%' COLLATE NOCASE
                    ORDER BY BinNVL LIMIT 50;";
                return DB_Base.GetData(sql, keyword, "key");
            }, ct);
        }

        /// <summary>Bảng: TTNVL, TTThanhPham, DanhSachMaSP</summary>
        public static async Task LoadLichSuSXAsync(string binNVL, DataGridView dtView)
        {
            if (string.IsNullOrWhiteSpace(binNVL)) return;
            UI.FrmWaiting waiting = null;
            try
            {
                waiting = new UI.FrmWaiting("Đang tải lịch sử sản xuất...");
                waiting.TopMost = true; waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show(); waiting.Refresh();

                var dt = await Task.Run(() =>
                {
                    const string sql = @"
                        SELECT nvl.TTThanhPham_ID AS ID, nvl.BinNVL AS lotNVL,
                            CAST(nvl.KlBatDau AS REAL) AS klBanDau, CAST(nvl.KlConLai AS REAL) AS klSau,
                            CAST(nvl.CdBatDau AS REAL) AS cdBanDau, CAST(nvl.CdConLai AS REAL) AS cdSau,
                            tp.MaBin AS lotTP, sp.Ten AS tenTP, tp.DateInsert AS ngay
                        FROM TTNVL nvl
                        INNER JOIN TTThanhPham tp ON tp.id = nvl.TTThanhPham_ID
                        LEFT JOIN DanhSachMaSP sp ON sp.id = tp.DanhSachSP_ID
                        WHERE nvl.BinNVL = @key ORDER BY nvl.TTThanhPham_ID DESC;";
                    return DB_Base.GetData(sql, binNVL, "key");
                });
                BindGridLichSuSX(dt, dtView);
            }
            catch (Exception ex) { MessageBox.Show("Không tải được lịch sử sản xuất.\n" + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { waiting?.SafeClose(); }
        }

        private static void BindGridLichSuSX(DataTable dt, DataGridView grv)
        {
            grv.AutoGenerateColumns = false;
            grv.DataSource = dt;
            foreach (string col in new[] { "klBanDau", "klSau", "cdBanDau", "cdSau" })
            {
                if (!grv.Columns.Contains(col)) continue;
                grv.Columns[col].DefaultCellStyle.Format = "N2";
                grv.Columns[col].DefaultCellStyle.FormatProvider = new CultureInfo("en-US");
                grv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        /// <summary>Bảng: LichSuSuaDoiThongTin</summary>
        public static Task<DataTable> SearchLichSuSuaDoiTheoLotAsync(string keyword, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                const string sql = @"
                    SELECT DISTINCT
                        CASE WHEN IFNULL(LOT_Moi, '') <> '' THEN LOT_Moi ELSE LOT_Cu END AS SoLot,
                        CASE WHEN IFNULL(LOT_Moi, '') <> '' AND IFNULL(LOT_Cu, '') <> '' AND LOT_Moi <> LOT_Cu
                             THEN LOT_Cu || ' -> ' || LOT_Moi
                             WHEN IFNULL(LOT_Moi, '') <> '' THEN LOT_Moi ELSE LOT_Cu END AS SoLotHienThi
                    FROM LichSuSuaDoiThongTin
                    WHERE (IFNULL(LOT_Cu, '') LIKE '%' || @key || '%' COLLATE NOCASE
                        OR IFNULL(LOT_Moi, '') LIKE '%' || @key || '%' COLLATE NOCASE)
                    ORDER BY SoLot DESC LIMIT 50;";
                return DB_Base.GetData(sql, keyword?.Trim(), "key");
            }, ct);
        }

        public static async Task LoadLichSuSuaDoiTheoLotAsync(string lot, DataGridView dtView)
        {
            if (string.IsNullOrWhiteSpace(lot)) return;
            UI.FrmWaiting waiting = null;
            try
            {
                waiting = new UI.FrmWaiting("Đang tải lịch sử sửa đổi...");
                waiting.TopMost = true; waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show(); waiting.Refresh();

                var dt = await Task.Run(() =>
                {
                    const string sql = @"
                        SELECT TTThanhPham_ID AS ID_lichSuSuDoi, Ten_Cu AS TenCu, Ten_Moi AS TenMoi,
                            LOT_Cu AS LotCu, LOT_Moi AS LotMoi, KL_Cu AS KlCu, KL_Moi AS KlMoi,
                            CD_Cu AS cdCu, CD_Moi AS cdMoi, DateInsert AS TGsua, TenMay AS tenMay,
                            GhiChu_Cu AS ghiChu_Cu, GhiChu_Moi AS ghiChu_Moi
                        FROM LichSuSuaDoiThongTin
                        WHERE IFNULL(LOT_Cu, '') = @key OR IFNULL(LOT_Moi, '') = @key
                        ORDER BY datetime(DateInsert) DESC, id DESC;";
                    return DB_Base.GetData(sql, lot.Trim(), "key");
                });
                BindGridLichSuSuaDoi(dt, dtView);
            }
            catch (Exception ex) { MessageBox.Show("Không tải được lịch sử sửa đổi.\n" + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { waiting?.SafeClose(); }
        }

        public static async Task LoadLichSuSuaDoiTheoSoNgayAsync(int soNgay, DataGridView dtView)
        {
            if (soNgay <= 0) return;
            UI.FrmWaiting waiting = null;
            try
            {
                waiting = new UI.FrmWaiting("Đang tải lịch sử sửa đổi...");
                waiting.TopMost = true; waiting.StartPosition = FormStartPosition.CenterScreen;
                waiting.Show(); waiting.Refresh();

                var dt = await Task.Run(() =>
                {
                    const string sql = @"
                        SELECT TTThanhPham_ID AS ID_lichSuSuDoi, Ten_Cu AS TenCu, Ten_Moi AS TenMoi,
                            LOT_Cu AS LotCu, LOT_Moi AS LotMoi, KL_Cu AS KlCu, KL_Moi AS KlMoi,
                            CD_Cu AS cdCu, CD_Moi AS cdMoi, DateInsert AS TGsua, TenMay AS tenMay,
                            GhiChu_Cu AS ghiChu_Cu, GhiChu_Moi AS ghiChu_Moi
                        FROM LichSuSuaDoiThongTin
                        WHERE date(DateInsert) >= date('now', '-' || (@soNgay - 1) || ' day')
                        ORDER BY datetime(DateInsert) DESC, id DESC;";

                    using var conn = DB_Base.OpenConnection();
                    using var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@soNgay", soNgay);
                    var result = new DataTable();
                    using var da = new SQLiteDataAdapter(cmd);
                    da.Fill(result);
                    return result;
                });
                BindGridLichSuSuaDoi(dt, dtView);
            }
            catch (Exception ex) { MessageBox.Show("Không tải được lịch sử sửa đổi.\n" + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { waiting?.SafeClose(); }
        }

        private static void BindGridLichSuSuaDoi(DataTable dt, DataGridView grv)
        {
            grv.AutoGenerateColumns = false;
            grv.DataSource = dt;
            foreach (string col in new[] { "KlCu", "KlMoi", "cdCu", "cdMoi" })
            {
                if (!grv.Columns.Contains(col)) continue;
                grv.Columns[col].DefaultCellStyle.Format = "N2";
                grv.Columns[col].DefaultCellStyle.FormatProvider = new CultureInfo("en-US");
                grv.Columns[col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        // ── Báo cáo SX ───────────────────────────────────────────────────────

        public static DataTable GetDataBaoCaoSX(DateTime ngayBatDau, DateTime ngayKetThuc, List<CongDoan> selectedCongDoans)
        {
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());
            var (sqlChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();
            string ngayBD = ngayBatDau.Date.AddHours(5).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss");
            string ngayKT = ngayKetThuc.Date.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
            string sqlDkNgay = $" WHERE date(tclv.Ngay) >= date('{ngayBD}') AND date(tclv.Ngay) <= date('{ngayKT}')";
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";
            string query = sqlSelect + " ," + sqlChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDkNgay + loaiCD + sqlOrder;
            return DB_Base.GetData(query);
        }

        public static DataTable GetTonKhoCD(List<CongDoan> selectedCongDoans)
        {
            string sqlSelect = CoreHelper.TaoSqL_LayThongTinBaoCaoChung();
            string sqlTenNVL = CoreHelper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());
            var (sqlChiTietCD, loaiCD) = CoreHelper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);
            string sqlJoin = CoreHelper.TaoSQL_TaoKetNoiCacBang();
            loaiCD = loaiCD.Replace("AND", "WHERE") + @"
                AND ((ds.DonVi = 'KG' AND ttp.KhoiLuongSau <> 0) OR (ds.DonVi = 'M' AND ttp.ChieuDaiSau <> 0))";
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";
            string query = sqlSelect + " ," + sqlChiTietCD + " ," + sqlTenNVL + sqlJoin + loaiCD + sqlOrder;
            return DB_Base.GetData(query);
        }

        public static DataTable GetThongTinNVLTheoMaBin(string mabin)
        {
            const string sql = @"
                SELECT TP.id AS STT, SP_NVL.Ten AS TenNVL, NVL.BinNVL as MaBin, SP_NVL.Ma AS MaNVL,
                    NVL.KlBatDau, NVL.CdBatDau, NVL.KlConLai, NVL.CdConLai,
                    NVL.DuongKinhSoiDong, NVL.SoSoi, NVL.KetCauLoi, NVL.DuongKinhSoiMach, NVL.BanRongBang, NVL.DoDayBang
                FROM TTThanhPham AS TP
                LEFT JOIN TTNVL AS NVL ON TP.id = NVL.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP AS SP_NVL ON NVL.DanhSachMaSP_ID = SP_NVL.id
                WHERE TP.MaBin = @mabin;";
            return DB_Base.GetData(sql, mabin, "mabin");
        }

        public static List<PrinterModel> GetPrinterDataByListBin(List<string> listBin)
        {
            var result = new List<PrinterModel>();
            var paramNames = listBin.Select((bin, index) => "@bin" + index).ToList();
            string query = $@"
                SELECT t.Ngay AS NgaySX, t.Ca AS CaSX, tp.QC, tp.KhoiLuongSau AS KhoiLuong, tp.ChieuDaiSau AS ChieuDai,
                    d.ten AS TenSP, tp.MaBin, d.ma AS MaSP, t.NguoiLam AS TenCN, tp.GhiChu AS GhiChu
                FROM TTThanhPham tp
                LEFT JOIN ThongTinCaLamViec t ON t.TTThanhPham_id = tp.id
                JOIN DanhSachMaSP d ON tp.DanhSachSP_ID = d.id
                WHERE tp.MaBin IN ({string.Join(",", paramNames)});";

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(query, conn);
            for (int i = 0; i < listBin.Count; i++) cmd.Parameters.AddWithValue("@bin" + i, listBin[i]);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new PrinterModel
                {
                    NgaySX = DateTime.TryParse(CoreHelper.GetString(reader, "NgaySX"), out DateTime d) ? d.ToString("dd/MM/yyyy") : "",
                    CaSX = CoreHelper.GetString(reader, "CaSX"),
                    KhoiLuong = CoreHelper.GetString(reader, "KhoiLuong"),
                    ChieuDai = CoreHelper.GetString(reader, "ChieuDai"),
                    TenSP = CoreHelper.GetString(reader, "TenSP"),
                    MaBin = CoreHelper.GetString(reader, "MaBin"),
                    MaSP = CoreHelper.GetString(reader, "MaSP"),
                    DanhGia = "",
                    QC = CoreHelper.GetString(reader, "QC").Trim(),
                    TenCN = CoreHelper.GetString(reader, "TenCN"),
                    GhiChu = CoreHelper.GetString(reader, "GhiChu")
                });
            }
            return result;
        }

        // ── Helper nội bộ Insert ─────────────────────────────────────────────

        internal static long InsertTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, List<TTNVL> nvl)
        {
            const string sql = @"
                INSERT INTO TTThanhPham (DanhSachSP_ID, QC, MaBin, KhoiLuongTruoc, KhoiLuongSau,
                    ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu, HanNoi, DateInsert)
                VALUES (@DanhSachSP_ID, @QC, @MaBin, @KhoiLuongTruoc, @KhoiLuongSau,
                    @ChieuDaiTruoc, @ChieuDaiSau, @Phe, @CongDoan, @GhiChu, @HanNoi, @DateInsert);
                SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@QC", m.QC);
            cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@Phe", m.Phe);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
            cmd.Parameters.AddWithValue("@DateInsert", (object?)m.DateInsert ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        internal static long InsertThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, long id)
        {
            const string sql = @"
                INSERT INTO ThongTinCaLamViec (Ngay, TTThanhPham_id, May, Ca, NguoiLam, ToTruong, QuanDoc)
                VALUES (@Ngay, @TTThanhPham_id, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
                SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@TTThanhPham_id", id);
            cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca);
            cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        internal static void InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            if (items == null || items.Count == 0) return;
            const string sql = @"
                INSERT INTO TTNVL (TTThanhPham_ID, BinNVL, QC, DanhSachMaSP_ID, KlBatDau, CdBatDau, KlConLai, CdConLai,
                    DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach, BanRongBang, DoDayBang)
                VALUES (@TTThanhPham_ID, @BinNVL, @QC, @DanhSachMaSP_ID, @KlBatDau, @CdBatDau, @KlConLai, @CdConLai,
                    @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach, @BanRongBang, @DoDayBang);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            var pId = cmd.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
            var pBin = cmd.Parameters.Add("@BinNVL", DbType.String);
            var pMaSP = cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int64);
            var pKlBD = cmd.Parameters.Add("@KlBatDau", DbType.Double);
            var pQC = cmd.Parameters.Add("@QC", DbType.String);
            var pCdBD = cmd.Parameters.Add("@CdBatDau", DbType.Double);
            var pKlCL = cmd.Parameters.Add("@KlConLai", DbType.Double);
            var pCdCL = cmd.Parameters.Add("@CdConLai", DbType.Double);
            var pDKSD = cmd.Parameters.Add("@DuongKinhSoiDong", DbType.Double);
            var pSS = cmd.Parameters.Add("@SoSoi", DbType.Int32);
            var pKCL = cmd.Parameters.Add("@KetCauLoi", DbType.Double);
            var pDKSM = cmd.Parameters.Add("@DuongKinhSoiMach", DbType.Double);
            var pBRB = cmd.Parameters.Add("@BanRongBang", DbType.Double);
            var pDDB = cmd.Parameters.Add("@DoDayBang", DbType.Double);

            foreach (TTNVL m in items)
            {
                pId.Value = thongTinSpId; pBin.Value = m.BinNVL ?? string.Empty;
                pMaSP.Value = m.DanhSachMaSP_ID; pKlBD.Value = m.KlBatDau; pQC.Value = m.QC;
                pCdBD.Value = m.CdBatDau; pKlCL.Value = m.KlConLai; pCdCL.Value = m.CdConLai;
                pDKSD.Value = m.DuongKinhSoiDong; pSS.Value = m.SoSoi; pKCL.Value = m.KetCauLoi;
                pDKSM.Value = m.DuongKinhSoiMach; pBRB.Value = m.BanRongBang; pDDB.Value = m.DoDayBang;
                cmd.ExecuteNonQuery();
            }
        }

        private static long InsertCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long tpId, CaiDatCDBoc m)
        {
            const string sql = @"
                INSERT INTO CaiDatCDBoc (TTThanhPham_ID, MangNuoc, PuliDanDay, BoDemMet, MayIn,
                    v1, v2, v3, v4, v5, v6, Co, Dau1, Dau2, Khuon, BinhSay,
                    DKKhuon1, DKKhuon2, TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
                    KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2)
                VALUES (@TTThanhPham_ID, @MangNuoc, @PuliDanDay, @BoDemMet, @MayIn,
                    @v1, @v2, @v3, @v4, @v5, @v6, @Co, @Dau1, @Dau2, @Khuon, @BinhSay,
                    @DKKhuon1, @DKKhuon2, @TTNhua, @NhuaPhe, @GhiChuNhuaPhe, @DayPhe, @GhiChuDayPhe,
                    @KTDKLan1, @KTDKLan2, @KTDKLan3, @DiemMongLan1, @DiemMongLan2);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", tpId);
            cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v1", (object?)m.v1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v2", (object?)m.v2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v3", (object?)m.v3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v4", (object?)m.v4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v5", (object?)m.v5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v6", (object?)m.v6 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Co", (object?)m.Co ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau1", (object?)m.Dau1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Dau2", (object?)m.Dau2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Khuon", (object?)m.Khuon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BinhSay", (object?)m.BinhSay ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1); cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
            cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
            cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
            cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
            cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1); cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2); cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
            cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1); cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);
            try { cmd.ExecuteNonQuery(); return conn.LastInsertRowId; }
            catch (Exception ex) { throw new Exception("Lỗi khi thêm dữ liệu vào bảng CaiDatCDBoc.", ex); }
        }

        private static void InsertCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_BocLot (CaiDatCDBoc_ID, DoDayTBLot) VALUES (@CaiDatCDBoc_ID, @DoDayTBLot);", conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id); cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_BocVo (CaiDatCDBoc_ID, DayVoTB, InAn) VALUES (@CaiDatCDBoc_ID, @DayVoTB, @InAn);", conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id); cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB); cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_BocMach (CaiDatCDBoc_ID, NgoaiQuan, LanDanhThung, SoMet, Mau) VALUES (@CaiDatCDBoc_ID, @NgoaiQuan, @LanDanhThung, @SoMet, @Mau);", conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id); cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1");
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung); cmd.Parameters.AddWithValue("@SoMet", m.SoMet); cmd.Parameters.AddWithValue("@Mau", m.Mau);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_KeoRut m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_KeoRut (TTThanhPham_ID, DKTrucX, DKTrucY, NgoaiQuan, TocDo, DienApU, DongDienU) VALUES (@TTThanhPham_ID, @DKTrucX, @DKTrucY, @NgoaiQuan, @TocDo, @DienApU, @DongDienU);", conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id); cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX); cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty); cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU); cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BenRuot m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_BenRuot (TTThanhPham_ID, DKSoi, SoSoi, ChieuXoan, BuocBen) VALUES (@TTThanhPham_ID, @DKSoi, @SoSoi, @ChieuXoan, @BuocBen);", conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id); cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value); cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z"); cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_GhepLoiQB m)
        {
            using var cmd = new SQLiteCommand("INSERT INTO CD_GhepLoiQB (TTThanhPham_ID, ChieuXoan, GoiCachMep, DKBTP) VALUES (@TTThanhPham_ID, @ChieuXoan, @GoiCachMep, @DKBTP);", conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id); cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep); cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }

        // ── Helper nội bộ Update ─────────────────────────────────────────────

        private static void UpdateKL_CD_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            const string sql = @"UPDATE TTThanhPham SET KhoiLuongSau = @KhoiLuongSau, ChieuDaiSau = @ChieuDaiSau, QC = @QC, LastEdit_ID = @LastEdit_ID WHERE MaBin = @MaBin;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.Add("@KhoiLuongSau", System.Data.DbType.Double); cmd.Parameters.Add("@ChieuDaiSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@LastEdit_ID", System.Data.DbType.Int64); cmd.Parameters.Add("@MaBin", System.Data.DbType.String);
            cmd.Parameters.Add("@QC", System.Data.DbType.String);
            foreach (var nvl in nvlList)
            {
                cmd.Parameters["@KhoiLuongSau"].Value = nvl.KlConLai; cmd.Parameters["@ChieuDaiSau"].Value = nvl.CdConLai;
                cmd.Parameters["@LastEdit_ID"].Value = thongTinSpId; cmd.Parameters["@QC"].Value = nvl.QC;
                cmd.Parameters["@MaBin"].Value = nvl.BinNVL;
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateKhoiLuongConLai_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvl, long tpId)
            => UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

        private static void Del_InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long tpId, List<TTNVL> nvl)
        {
            using (var del = new SQLiteCommand("DELETE FROM TTNVL WHERE TTThanhPham_ID = @id;", conn, tx))
            { del.Parameters.AddWithValue("@id", tpId); del.ExecuteNonQuery(); }
            InsertTTNVL(conn, tx, tpId, nvl);
        }

        private static void RestoreFromNVL(SQLiteConnection conn, SQLiteTransaction tx, long tpId)
        {
            const string sql = @"
                UPDATE TTThanhPham AS tp
                SET KhoiLuongSau = (SELECT nvl.KlBatDau FROM TTNVL AS nvl WHERE nvl.TTThanhPham_ID = tp.LastEdit_id AND nvl.BinNVL = tp.MaBin),
                    ChieuDaiSau  = (SELECT nvl.CdBatDau FROM TTNVL AS nvl WHERE nvl.TTThanhPham_ID = tp.LastEdit_id AND nvl.BinNVL = tp.MaBin),
                    LastEdit_id  = NULL
                WHERE tp.LastEdit_id = @tpId
                  AND EXISTS (SELECT 1 FROM TTNVL AS nvl WHERE nvl.TTThanhPham_ID = tp.LastEdit_id AND nvl.BinNVL = tp.MaBin);";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@tpId", tpId);
            cmd.ExecuteNonQuery();
        }

        private static void BackupThongTinTruocKhiSua(SQLiteConnection conn, SQLiteTransaction tx, long tpId, TTThanhPham tp, string nguoiLam)
        {
            // logic giữ nguyên từ file gốc — lưu vào LichSuSuaDoiThongTin
            const string sql = @"
                INSERT INTO LichSuSuaDoiThongTin
                (TTThanhPham_ID, Ten_Cu, Ten_Moi, LOT_Cu, LOT_Moi, KL_Cu, KL_Moi, CD_Cu, CD_Moi, DateInsert, TenMay, GhiChu_Cu, GhiChu_Moi)
                SELECT id, DanhSachSP_ID, @TenMoi, MaBin, @LotMoi, KhoiLuongSau, @KLMoi, ChieuDaiSau, @CDMoi, @DateInsert, @TenMay, GhiChu, @GhiChuMoi
                FROM TTThanhPham WHERE id = @tpId;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@tpId", tpId);
            cmd.Parameters.AddWithValue("@TenMoi", (object?)tp.TenTP ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LotMoi", (object?)tp.MaBin ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KLMoi", (object?)tp.KhoiLuongSau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CDMoi", (object?)tp.ChieuDaiSau ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GhiChuMoi", (object?)tp.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateInsert", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@TenMay", nguoiLam);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, long tpId)
        {
            const string sql = @"UPDATE ThongTinCaLamViec SET Ngay=@Ngay, May=@May, Ca=@Ca, NguoiLam=@NguoiLam, ToTruong=@ToTruong, QuanDoc=@QuanDoc WHERE TTThanhPham_id=@id;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay); cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca); cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", tpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, long tpId, List<TTNVL> nvl)
        {
            const string sql = @"UPDATE TTThanhPham SET DanhSachSP_ID=@DanhSachSP_ID, MaBin=@MaBin, Phe=@Phe, CongDoan=@CongDoan, GhiChu=@GhiChu, QC=@QC WHERE id=@id;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID); cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@Phe", m.Phe); cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QC", m.QC); cmd.Parameters.AddWithValue("@id", tpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long tpId, CaiDatCDBoc m)
        {
            // Update đơn giản các field chính — giữ nguyên logic gốc
            const string sql = @"UPDATE CaiDatCDBoc SET MangNuoc=@MangNuoc, PuliDanDay=@PuliDanDay, BoDemMet=@BoDemMet, MayIn=@MayIn WHERE TTThanhPham_ID=@id;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@MangNuoc", (object?)m.MangNuoc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PuliDanDay", (object?)m.PuliDanDay ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BoDemMet", (object?)m.BoDemMet ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MayIn", (object?)m.MayIn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", tpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"UPDATE CD_BocMach SET NgoaiQuan=@NgoaiQuan, LanDanhThung=@LanDanhThung, SoMet=@SoMet WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan); cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet); cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_KeoRut m)
        {
            const string sql = @"UPDATE CD_KeoRut SET DKTrucX=@DKTrucX, DKTrucY=@DKTrucY, NgoaiQuan=@NgoaiQuan, TocDo=@TocDo, DienApU=@DienApU, DongDienU=@DongDienU WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX); cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty); cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU); cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BenRuot m)
        {
            const string sql = @"UPDATE CD_BenRuot SET DKSoi=@DKSoi, SoSoi=@SoSoi, ChieuXoan=@ChieuXoan, BuocBen=@BuocBen WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi); cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z"); cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_GhepLoiQB m)
        {
            const string sql = @"UPDATE CD_GhepLoiQB SET ChieuXoan=@ChieuXoan, GoiCachMep=@GoiCachMep, DKBTP=@DKBTP WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z"); cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP); cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"UPDATE CD_BocLot SET DoDayTBLot=@DoDayTBLot WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot); cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"UPDATE CD_BocVo SET DayVoTB=@DayVoTB, InAn=@InAn WHERE TTThanhPham_ID=@TTThanhPham_ID;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB); cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }
    }
}