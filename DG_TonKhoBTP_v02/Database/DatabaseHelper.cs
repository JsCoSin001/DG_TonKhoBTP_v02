
using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.DL_Ben;
using DG_TonKhoBTP_v02.Models;
using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Database
{
    public static class DatabaseHelper
    {
        public static string _connStr;

        // Thiết lập đường dẫn đến cơ sở dữ liệu SQLite
        public static void SetDatabasePath(string path)
        {
            _connStr = $"Data Source={path};Version=3;";
        }

        public static string GetStringConnector
        {
            get { return _connStr; }
        }

        #region Lấy dữ liệu
        // Lấy dữ liệu theo 1 điều kiện
        public static DataTable GetData(string query , string key = null,  string para = null)
        {
            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(para) && key != null)
                    {
                        cmd.Parameters.AddWithValue("@" + para, key);
                    }

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        public static DataTable GetDataByCongDoan(DateTime selectedDate, CongDoan cd,int ca, string nguoiKiemTra)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");

            string sqlSelect = Helper.Helper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = Helper.Helper.TaoSQL_LayChiTiet_1CD(cd.Id);

            string sqlTenNVL = Helper.Helper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = Helper.Helper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE date(tclv.Ngay) = date(@para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            string sqlDk3 = " AND tclv.Ca = " + ca;

            if (!string.IsNullOrWhiteSpace(nguoiKiemTra))
            {
                sqlDk3 += " AND tclv.NguoiLam = '" + nguoiKiemTra + "' ";
            }

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlDk3 + sqlOrder;

            return GetData(query, key, "para");
        }

        // Lấy dữ liệu theo ngày tháng
        public static DataTable GetDataByMonth(DateTime selectedDate, CongDoan cd)
        {
            string key = selectedDate.ToString("yyyy-MM-dd");
            
            string sqlSelect = Helper.Helper.TaoSqL_LayThongTinBaoCaoChung();

            string sqlLayChiTietCD = Helper.Helper.TaoSQL_LayChiTiet_1CD(cd.Id);
            
            string sqlTenNVL = Helper.Helper.TaoSQL_LayDuLieuNVL(cd.Columns);

            string sqlJoin = Helper.Helper.TaoSQL_TaoKetNoiCacBang();

            string sqlDk1 = " WHERE strftime('%Y-%m', tclv.Ngay) = strftime('%Y-%m', @para) ";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            // 6) ORDER BY
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // 7) Kết hợp hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 + sqlDk2 + sqlOrder;

            return GetData(query, key, "para");
        }
        // Lấy dữ liệu theo ID
        public static DataTable GetDataByID(string key, CongDoan cd)
        {
            // Tạo select
            string sqlSelect = Helper.Helper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy thông tin chi tiết công đoạn
            string sqlLayChiTietCD = Helper.Helper.TaoSQL_LayChiTiet_1CD(cd.Id);

            // Lấy dữ liệu nvl theo công đoạn
            string sqlTenNVL = Helper.Helper.TaoSQL_LayDuLieuNVL(cd.Columns);

            // Tạo câu nối các bảng
            string sqlJoin = Helper.Helper.TaoSQL_TaoKetNoiCacBang();

            // Tạo điều kiện lọc theo ID
            string sqlDk1 = " WHERE ttp.id = @id";

            string sqlDk2 = " AND ttp.CongDoan = " + cd.Id;

            // Kết hợp câu truy vấn
            string query = sqlSelect + " ,"+ sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDk1 +sqlDk2;

            return GetData(query, key, "id");
        }
        // Lấy báo cáo sản xuất
        public static DataTable GetDataBaoCaoSX(DateTime ngayBatDau, DateTime ngayKetThuc, List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = Helper.Helper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = Helper.Helper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var(sqlLayChiTietCD, loaiCD) = Helper.Helper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans) ;


            // Câu nối các bảng
            string sqlJoin = Helper.Helper.TaoSQL_TaoKetNoiCacBang();

            // Format ngày sang dạng SQLite hiểu được
            string ngayBD = ngayBatDau.Date.AddHours(5).AddMinutes(59).ToString("yyyy-MM-dd HH:mm:ss");

            string ngayKT = ngayKetThuc.Date.AddDays(1).AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");

            // Điều kiện WHERE – chèn trực tiếp giá trị ngày
            string sqlDkNgay = $" WHERE date(tclv.Ngay) >= date('{ngayBD}') AND date(tclv.Ngay) <= date('{ngayKT}')";


            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin + sqlDkNgay + loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetTonKhoCD( List<CongDoan> selectedCongDoans)
        {
            // Tạo phần SELECT chung
            string sqlSelect = Helper.Helper.TaoSqL_LayThongTinBaoCaoChung();

            // Lấy dữ liệu NVL theo danh sách công đoạn
            string sqlTenNVL = Helper.Helper.TaoSQL_LayDuLieuNVL(selectedCongDoans.Select(cd => cd.Columns).ToArray());

            // Lấy chi tiết công đoạn
            var (sqlLayChiTietCD, loaiCD) = Helper.Helper.TaoSQL_LayChiTiet_NhieuCD(selectedCongDoans);

            // Câu nối các bảng
            string sqlJoin = Helper.Helper.TaoSQL_TaoKetNoiCacBang();

            loaiCD = loaiCD.Replace("AND", "WHERE")
                    + @"
                        AND (
                        (ds.DonVi = 'KG' AND ttp.KhoiLuongSau > 0)
                        OR
                        (ds.DonVi = 'M' AND ttp.ChieuDaiSau > 0)
                      )";


            // Sắp xếp
            string sqlOrder = " ORDER BY tclv.Ngay DESC, ttp.id DESC;";

            // Ghép chuỗi hoàn chỉnh
            string query = sqlSelect + " ," + sqlLayChiTietCD + " ," + sqlTenNVL + sqlJoin +  loaiCD + sqlOrder;

            return GetData(query);
        }

        public static DataTable GetThongTinNVLTheoMaBin(string mabin)
        {
            string query = @"
                SELECT                     
                    TP.id AS STT,
                    SP_NVL.Ten AS TenNVL,
                    NVL.BinNVL as MaBin,
                    SP_NVL.Ma AS MaNVL,
                    NVL.KlBatDau,
                    NVL.CdBatDau,
                    NVL.KlConLai,
                    NVL.CdConLai,
                    NVL.DuongKinhSoiDong,
                    NVL.SoSoi,
                    NVL.KetCauLoi,
                    NVL.DuongKinhSoiMach,
                    NVL.BanRongBang,
                    NVL.DoDayBang
                FROM TTThanhPham AS TP
                LEFT JOIN TTNVL AS NVL ON TP.id = NVL.TTThanhPham_ID
                LEFT JOIN DanhSachMaSP AS SP_NVL ON NVL.DanhSachMaSP_ID = SP_NVL.id
                WHERE TP.MaBin = @mabin;
            ";

            return GetData(query, mabin, "mabin");
        }

        public static List<PrinterModel> GetPrinterDataByListBin(List<string> listBin)
        {
            List<PrinterModel> result = new List<PrinterModel>();


            // 1. Tạo danh sách parameter @bin0, @bin1,...
            var paramNames = listBin.Select((bin, index) => "@bin" + index).ToList();
            string inClause = string.Join(",", paramNames);

            // 2. SQL truy vấn
            string query = $@"
                SELECT  
                    t.Ngay AS NgaySX,
                    t.Ca AS CaSX,
                    tp.KhoiLuongSau AS KhoiLuong,
                    tp.ChieuDaiSau AS ChieuDai,
                    d.ten AS TenSP,
                    tp.MaBin AS MaBin,
                    d.ma AS MaSP,
                    t.NguoiLam AS TenCN,
                    tp.GhiChu AS GhiChu
                FROM TTThanhPham tp
                JOIN ThongTinCaLamViec t ON tp.ThongTinCaLamViec_ID = t.id
                JOIN DanhSachMaSP d ON tp.DanhSachSP_ID = d.id
                WHERE tp.MaBin IN ({inClause});
                ";

            using (SQLiteConnection conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    // 3. Gán giá trị cho từng parameter
                    for (int i = 0; i < listBin.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@bin" + i, listBin[i]);
                    }

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new PrinterModel
                            {
                                NgaySX = DateTime.Parse(reader["NgaySX"].ToString()).ToString("dd/MM/yyyy"),
                                CaSX = reader["CaSX"].ToString(),
                                KhoiLuong = reader["KhoiLuong"].ToString(),
                                ChieuDai = reader["ChieuDai"].ToString(),
                                TenSP = reader["TenSP"].ToString(),
                                MaBin = reader["MaBin"].ToString(),
                                MaSP = reader["MaSP"].ToString(),
                                DanhGia = "",
                                TenCN = reader["TenCN"].ToString(),
                                GhiChu = reader["GhiChu"].ToString()
                            };

                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }


        public static ConfigDB GetConfig()
        {

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                string sql = "SELECT Active, Message FROM ConfigDB LIMIT 1";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ConfigDB
                        {
                            Active = reader.IsDBNull(0) ? false : reader.GetBoolean(0),
                            Message = reader.IsDBNull(1) ? null : reader.GetString(1)
                        };
                    }
                }
            }

            return null; // Trường hợp bảng rỗng
        }
        #endregion

        #region Update dữ liệu
        public static bool UpdateNguoiKiemTra(List<int> listStt, string nguoiKT)
        {
            if (listStt == null || listStt.Count == 0)
                return false;

            try
            {
                using (var con = new SQLiteConnection(_connStr))
                {
                    con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        // tạo param hàng loạt @p0, @p1, ...
                        List<string> paramNames = new List<string>();
                        int i = 0;

                        foreach (int stt in listStt)
                        {
                            string p = "@p" + i;
                            paramNames.Add(p);
                            cmd.Parameters.AddWithValue(p, stt);
                            i++;
                        }

                        // param tên tổ trưởng
                        cmd.Parameters.AddWithValue("@nguoiKT", nguoiKT);

                        cmd.CommandText = $@"
                            UPDATE ThongTinCaLamViec
                            SET ToTruong = @nguoiKT
                            WHERE id IN (
                                SELECT ThongTinCaLamViec_ID
                                FROM TTThanhPham
                                WHERE id IN ({string.Join(",", paramNames)})
                            );
                            ";
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
               return false;
            }
        }


        private static void UpdateKL_CD_TTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList, long thongTinSpId)
        {
            const string sql = @"
                UPDATE TTThanhPham
                SET KhoiLuongSau = @KhoiLuongSau,
                    ChieuDaiSau = @ChieuDaiSau,
                    LastEdit_ID = @LastEdit_ID
                WHERE MaBin = @MaBin;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.Add("@KhoiLuongSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@ChieuDaiSau", System.Data.DbType.Double);
            cmd.Parameters.Add("@LastEdit_ID", System.Data.DbType.Int64);
            cmd.Parameters.Add("@MaBin", System.Data.DbType.String);

            foreach (var nvl in nvlList)
            {
                cmd.Parameters["@KhoiLuongSau"].Value = nvl.KlConLai;
                cmd.Parameters["@ChieuDaiSau"].Value = nvl.CdConLai;
                cmd.Parameters["@LastEdit_ID"].Value = thongTinSpId;
                cmd.Parameters["@MaBin"].Value = nvl.BinNVL;

                cmd.ExecuteNonQuery(); 
            }
        }

        // Main update function
        public static bool UpdateDataSanPham(int tpId, ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            using var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using var tx = conn.BeginTransaction();

            CaiDatCDBoc caiDat = (CaiDatCDBoc)chiTietCD[1];

            errorMsg = string.Empty;

            try
            {
                // 1) ThongTinCaLamViec
                UpdateThongTinCaLamViec(conn, tx, caLam, tpId);

                // 2) TTThanhPham
                UpdateTTThanhPham(conn, tx, tp, tpId);

                // 3) TTNVL -> Xoá nvl cũ và Tạo mới
                Del_InsertTTNVL(conn, tx, tpId, nvl);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKhoiLuongConLai_TTThanhPham(conn, tx, nvl, tpId);

                // 4) CaiDatCDBoc
                if (caiDat != null) UpdateCaiDatCDBoc(conn, tx, tpId, caiDat);

                // 5) Thêm chi tiết các công đoạn
                switch (chiTietCD[0])
                {
                    case CD_BenRuot ben:
                        UpdateCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_KeoRut keo:
                        UpdateCDKeoRut(conn, tx, tpId, keo);
                        updateVersion1(tpId, tp, caLam);
                        break;

                    case CD_GhepLoiQB qb:
                        UpdateCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocMach mach:
                        UpdateCDBocMach(conn, tx, tpId, mach);
                        break;

                    case CD_BocLot lot:
                        UpdateCDBocLot(conn, tx, tpId, lot);
                        break;

                    case CD_BocVo vo:
                        UpdateCDBocVo(conn, tx, tpId, vo);
                        break;




                    default:
                        throw new ArgumentException("Lỗi bất thường.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();

                errorMsg = Helper.Helper.ShowErrorDatabase(ex, tp.MaBin);

                return false;
            }

        }

        private static void UpdateThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m, int id)
        {
            string sqlUpdate = @"UPDATE ThongTinCaLamViec 
                        SET Ngay = @Ngay,
                            May = @May,
                            Ca = @Ca,
                            NguoiLam = @NguoiLam,
                            ToTruong = @ToTruong,
                            QuanDoc = @QuanDoc
                        WHERE id = (SELECT ThongTinCaLamViec_ID 
                                   FROM TTThanhPham 
                                   WHERE id = @id)";

            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
                cmd.Parameters.AddWithValue("@May", m.May);
                cmd.Parameters.AddWithValue("@Ca", m.Ca);
                cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
                cmd.Parameters.AddWithValue("@ToTruong", m.ToTruong ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@QuanDoc", m.QuanDoc ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update ThongTinCaLamViec cho TTThanhPham id = {id}");
                }
            }
        }

        private static void UpdateTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, int thongTinCaLamViecId)
        {
            string sqlUpdate = @"UPDATE TTThanhPham 
                                SET DanhSachSP_ID = @DanhSachSP_ID,
                                    MaBin = @MaBin,
                                    KhoiLuongTruoc = @KhoiLuongTruoc,
                                    KhoiLuongSau = @KhoiLuongSau,
                                    ChieuDaiTruoc = @ChieuDaiTruoc,
                                    ChieuDaiSau = @ChieuDaiSau,
                                    Phe = @Phe,
                                    GhiChu = @GhiChu
                                WHERE id = @id";
            m.GhiChu = m.GhiChu + "- Đã sửa";

            using (var cmd = new SQLiteCommand(sqlUpdate, conn, tx))
            {
                cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
                cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
                cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
                cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
                cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
                cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
                cmd.Parameters.AddWithValue("@Phe", m.Phe);
                cmd.Parameters.AddWithValue("@GhiChu", m.GhiChu ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", thongTinCaLamViecId);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    throw new Exception($"Không tìm thấy hoặc không thể update TTThanhPham với id = {m.Id}");
                }
            }
        }

        private static void Del_InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            // Xoá dữ liệu cũ
            using (var cmd = new SQLiteCommand(conn))
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"DELETE FROM TTNVL WHERE TTThanhPham_ID = @TTThanhPham_ID";
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
                cmd.ExecuteNonQuery();
            }
            // Thêm dữ liệu mới
            InsertTTNVL(conn, tx, thongTinSpId, items);
        }

        private static void UpdateKhoiLuongConLai_TTThanhPham( SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> nvlList,long thongTinSpId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (nvlList == null || nvlList.Count == 0) return;

            using var cmd = new SQLiteCommand(@"
            UPDATE TTThanhPham
               SET KhoiLuongSau = @kl,
                   ChieuDaiSau  = @cd
             WHERE MaBin       = @mabin
               AND LastEdit_id = @lastEditId;", conn, tx);

            var pKL = cmd.Parameters.Add("@kl", DbType.Double);
            var pCD = cmd.Parameters.Add("@cd", DbType.Double);
            var pBin = cmd.Parameters.Add("@mabin", DbType.String);
            var pLE = cmd.Parameters.Add("@lastEditId", DbType.Int64);

            pLE.Value = thongTinSpId;

            foreach (var nvl in nvlList)
            {
                if (nvl == null || string.IsNullOrWhiteSpace(nvl.BinNVL))
                    continue;

                pKL.Value = nvl.KlConLai;
                pCD.Value = nvl.CdConLai;
                pBin.Value = nvl.BinNVL.Trim();

                cmd.ExecuteNonQuery(); 
            }
        }

        private static void UpdateCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long id, CaiDatCDBoc m)
        {
            string query = @"
                UPDATE CaiDatCDBoc
                SET 
                    MangNuoc = @MangNuoc,
                    PuliDanDay = @PuliDanDay,
                    BoDemMet = @BoDemMet,
                    MayIn = @MayIn,
                    v1 = @v1,
                    v2 = @v2,
                    v3 = @v3,
                    v4 = @v4,
                    v5 = @v5,
                    v6 = @v6,
                    Co = @Co,
                    Dau1 = @Dau1,
                    Dau2 = @Dau2,
                    Khuon = @Khuon,
                    BinhSay = @BinhSay,
                    DKKhuon1 = @DKKhuon1,
                    DKKhuon2 = @DKKhuon2,
                    TTNhua = @TTNhua,
                    NhuaPhe = @NhuaPhe,
                    GhiChuNhuaPhe = @GhiChuNhuaPhe,
                    DayPhe = @DayPhe,
                    GhiChuDayPhe = @GhiChuDayPhe,
                    KTDKLan1 = @KTDKLan1,
                    KTDKLan2 = @KTDKLan2,
                    KTDKLan3 = @KTDKLan3,
                    DiemMongLan1 = @DiemMongLan1,
                    DiemMongLan2 = @DiemMongLan2
                WHERE TTThanhPham_ID = @TTThanhPham_ID;
            ";

            using (var cmd = new SQLiteCommand(query, conn, tx))
            {
                cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);


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

                cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1);
                cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
                cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
                cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
                cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
                cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1);
                cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2);
                cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
                cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1);
                cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);

                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
                UPDATE CD_BocLot
                SET DoDayTBLot = @DoDayTBLot
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }
        
        private static void UpdateCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
                UPDATE CD_BocVo
                SET DayVoTB = @DayVoTB,
                    InAn = @InAn
                WHERE CaiDatCDBoc_ID IN (
                    SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
                );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            UPDATE CD_BocMach
            SET 
                NgoaiQuan = @NgoaiQuan,
                LanDanhThung = @LanDanhThung,
                SoMet = @SoMet
            WHERE CaiDatCDBoc_ID IN (
                SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID = @TTThanhPham_ID
            );";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1");
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", id);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            UPDATE CD_KeoRut
            SET DKTrucX = @DKTrucX,
                DKTrucY = @DKTrucY,
                NgoaiQuan = @NgoaiQuan,
                TocDo = @TocDo,
                DienApU = @DienApU,
                DongDienU = @DongDienU
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            const string sql = @"
            UPDATE CD_BenRuot
            SET DKSoi = @DKSoi,
                SoSoi = @SoSoi,
                ChieuXoan = @ChieuXoan,
                BuocBen = @BuocBen
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            const string sql = @"
            UPDATE CD_GhepLoiQB
            SET BuocXoan = @BuocXoan,
                ChieuXoan = @ChieuXoan,
                GoiCachMep = @GoiCachMep,
                DKBTP = @DKBTP
            WHERE TTThanhPham_ID = @TTThanhPham_ID;";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.ExecuteNonQuery();
        }

        public static string UpdateDanhSachMaSP(DanhSachMaSP sp, int key)
        {
            string query = @"
                UPDATE DanhSachMaSP
                SET 
                    Ten = @Ten,
                    Ma = @Ma,
                    DonVi = @DonVi,
                    KieuSP = @KieuSP,
                    DateInsert = @DateInsert
                WHERE id = @Id;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                        cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                        cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                        cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                        cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert);
                        cmd.Parameters.AddWithValue("@Id", key);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        // Nếu cập nhật thành công → trả về chuỗi rỗng
                        if (rowsAffected > 0)
                            return "";
                        else
                            return $"Không tìm thấy bản ghi với Tên = {sp.Ten}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Trả về lỗi từ helper
                return Helper.Helper.ShowErrorDatabase(ex, sp.Ten);
            }
        }

        public static string UpdateKLConLai_BanTran(BanTran bt)
        {
            string query = @"
                UPDATE TTThanhPham
                SET 
                    KhoiLuongSau = @KhoiLuongSau,
                    KLBanTran = COALESCE(KLBanTran, 0) + @KhoiLuongBanTran
                WHERE MaBin = @MaBin;
            ";

            try
            {
                using (var conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KhoiLuongSau", bt.KhoiLuongSau);
                        cmd.Parameters.AddWithValue("@KhoiLuongBanTran", bt.KhoiLuongBanTran);
                        cmd.Parameters.AddWithValue("@MaBin", bt.MaBin);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return ""; // cập nhật thành công → trả về chuỗi rỗng
                        else
                            return $"Không tìm thấy bản ghi với MaBin = {bt.MaBin}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Gọi helper hiển thị lỗi giống phong cách bạn đang dùng
                return Helper.Helper.ShowErrorDatabase(ex, bt.MaBin);
            }
        }
        #endregion

        #region Insert dữ liệu các công đoạn
        public static bool SaveDataSanPham( ThongTinCaLamViec caLam, TTThanhPham tp, List<TTNVL> nvl, List<object> chiTietCD, out string errorMsg)
        {
            errorMsg = string.Empty;

            // Guard clauses
            if (tp == null)
            {
                errorMsg = "Thiếu thông tin thành phẩm.";
                return false;
            }
            if (chiTietCD == null || chiTietCD.Count == 0 || chiTietCD[0] == null)
            {
                errorMsg = "Thiếu chi tiết công đoạn.";
                return false;
            }

            long idCaiDatCDBoc = 0;
            using var conn = new SQLiteConnection(_connStr);
            SQLiteTransaction tx = null;

            try
            {
                conn.Open();                  // ✅ MỞ TRƯỚC
                tx = conn.BeginTransaction(); // ✅ RỒI MỚI BẮT ĐẦU TRANSACTION

                // 1) ThongTinCaLamViec
                long caId = InsertThongTinCaLamViec(conn, tx, caLam);

                // 2) TTThanhPham
                long tpId = InsertTTThanhPham(conn, tx, tp, caId);

                // 3) TTNVL -> Tạo mới
                InsertTTNVL(conn, tx, tpId, nvl);

                // 3.1) Update Khối lượng sau, Chiều dài sau và thêm ID được update ở TTThanhPham 
                UpdateKL_CD_TTThanhPham(conn, tx, nvl, tpId);

                // Dùng cho db v1
                //DatabasehelperVer01.UpdateTonKho_NVL_Lan1(nvl);

                // 4) CaiDatCDBoc (chỉ áp dụng cho nhóm bóc)
                var congDoan = chiTietCD[0];

                if (congDoan is CD_BocLot || congDoan is CD_BocVo || congDoan is CD_BocMach)
                {
                    // chỉ lấy caiDat nếu có phần tử thứ 2 và đúng kiểu
                    CaiDatCDBoc caiDat = (chiTietCD.Count > 1) ? chiTietCD[1] as CaiDatCDBoc : null;
                    if (caiDat != null)
                        idCaiDatCDBoc = InsertCaiDatCDBoc(conn, tx, tpId, caiDat);
                }

                // 5) Thêm chi tiết các công đoạn
                switch (congDoan)
                {
                    case CD_KeoRut keo:
                        InsertCDKeoRut(conn, tx, tpId, keo);
                        insertVersion1(tp,caLam, nvl);
                        break;

                    case CD_BenRuot ben:
                        InsertCDBenRuot(conn, tx, tpId, ben);
                        break;

                    case CD_GhepLoiQB qb:
                        InsertCDGhepLoiQB(conn, tx, tpId, qb);
                        break;

                    case CD_BocLot lot:
                        InsertCDBocLot(conn, tx, idCaiDatCDBoc, lot);
                        break;

                    case CD_BocMach mach:
                        InsertCDBocMach(conn, tx, idCaiDatCDBoc, mach);
                        break;

                    case CD_BocVo vo:
                        InsertCDBocVo(conn, tx, idCaiDatCDBoc, vo);
                        break;

                    default:
                        throw new ArgumentException("Lỗi bất thường: Công đoạn không hợp lệ.");
                }

                tx.Commit(); // ✅ nhớ commit
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* nuốt lỗi rollback để không che mất lỗi chính */ }

                errorMsg = Helper.Helper.ShowErrorDatabase(ex, tp.MaBin);
                return false;
            }
        }

        private static long InsertThongTinCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, ThongTinCaLamViec m)
        {
            const string sql = @"
            INSERT INTO ThongTinCaLamViec (Ngay, May, Ca, NguoiLam, ToTruong, QuanDoc)
            VALUES (@Ngay, @May, @Ca, @NguoiLam, @ToTruong, @QuanDoc);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@Ngay", m.Ngay);
            cmd.Parameters.AddWithValue("@May", m.May);
            cmd.Parameters.AddWithValue("@Ca", m.Ca);
            cmd.Parameters.AddWithValue("@NguoiLam", m.NguoiLam);
            cmd.Parameters.AddWithValue("@ToTruong", (object?)m.ToTruong ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@QuanDoc", (object?)m.QuanDoc ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static long InsertTTThanhPham(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m, long thongTinCaLamViecId)
        {
            const string sql = @"
            INSERT INTO TTThanhPham
                (DanhSachSP_ID, ThongTinCaLamViec_ID, MaBin, KhoiLuongTruoc, KhoiLuongSau, ChieuDaiTruoc, ChieuDaiSau, Phe, CongDoan, GhiChu, DateInsert)
            VALUES
                (@DanhSachSP_ID, @ThongTinCaLamViec_ID, @MaBin, @KhoiLuongTruoc, @KhoiLuongSau, @ChieuDaiTruoc, @ChieuDaiSau, @Phe, @CongDoan, @GhiChu, @DateInsert);
            SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@ThongTinCaLamViec_ID", thongTinCaLamViecId);
            cmd.Parameters.AddWithValue("@MaBin", m.MaBin);
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@Phe", m.Phe);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", (object?)m.GhiChu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateInsert", (object?)m.DateInsert ?? DBNull.Value);
            return (long)(cmd.ExecuteScalar() ?? 0L);
        }

        private static void InsertTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, List<TTNVL> items)
        {
            if (items == null || items.Count == 0) return;

            const string sql = @"
            INSERT INTO TTNVL
                (TTThanhPham_ID, BinNVL, DanhSachMaSP_ID, KlBatDau, CdBatDau, KlConLai, CdConLai, DuongKinhSoiDong, SoSoi, KetCauLoi, DuongKinhSoiMach, BanRongBang, DoDayBang)
            VALUES
                (@TTThanhPham_ID, @BinNVL,@DanhSachMaSP_ID, @KlBatDau, @CdBatDau, @KlConLai, @CdConLai, @DuongKinhSoiDong, @SoSoi, @KetCauLoi, @DuongKinhSoiMach, @BanRongBang, @DoDayBang);";

            using var cmd = new SQLiteCommand(sql, conn, tx);

            var pThongTinSP_ID = cmd.Parameters.Add("@TTThanhPham_ID", DbType.Int64);
            var pBinNVL = cmd.Parameters.Add("@BinNVL", DbType.String);
            var DanhSachMaSP_ID = cmd.Parameters.Add("@DanhSachMaSP_ID", DbType.Int64);
            var KlBatDau = cmd.Parameters.Add("@KlBatDau", DbType.Double);
            var CdBatDau = cmd.Parameters.Add("@CdBatDau", DbType.Double);
            var KlConLai = cmd.Parameters.Add("@KlConLai", DbType.Double);
            var CdConLai = cmd.Parameters.Add("@CdConLai", DbType.Double);
            var pDuongKinhSoiDong = cmd.Parameters.Add("@DuongKinhSoiDong", DbType.Double);
            var pSoSoi = cmd.Parameters.Add("@SoSoi", DbType.Int32);
            var pKetCauLoi = cmd.Parameters.Add("@KetCauLoi", DbType.Double);
            var pDuongKinhSoiMach = cmd.Parameters.Add("@DuongKinhSoiMach", DbType.Double);
            var pBanRongBang = cmd.Parameters.Add("@BanRongBang", DbType.Double);
            var pDoDayBang = cmd.Parameters.Add("@DoDayBang", DbType.Double);

            foreach (TTNVL m in items)
            {
                pThongTinSP_ID.Value = thongTinSpId;
                pBinNVL.Value = m.BinNVL ?? string.Empty;
                DanhSachMaSP_ID.Value = m.DanhSachMaSP_ID;
                KlBatDau.Value = m.KlBatDau;
                CdBatDau.Value = m.CdBatDau;
                KlConLai.Value = m.KlConLai;
                CdConLai.Value = m.CdConLai;
                pDuongKinhSoiDong.Value = m.DuongKinhSoiDong;
                pSoSoi.Value = m.SoSoi;
                pKetCauLoi.Value = m.KetCauLoi;
                pDuongKinhSoiMach.Value = m.DuongKinhSoiMach;
                pBanRongBang.Value = m.BanRongBang;
                pDoDayBang.Value = m.DoDayBang;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertCDBocLot(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocLot m)
        {
            const string sql = @"
            INSERT INTO CD_BocLot (CaiDatCDBoc_ID, DoDayTBLot)
            VALUES (@CaiDatCDBoc_ID, @DoDayTBLot);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DoDayTBLot", m.DoDayTBLot);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBocVo(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocVo m)
        {
            const string sql = @"
            INSERT INTO CD_BocVo (CaiDatCDBoc_ID, DayVoTB, InAn)
            VALUES (@CaiDatCDBoc_ID, @DayVoTB, @InAn);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@DayVoTB", m.DayVoTB);
            cmd.Parameters.AddWithValue("@InAn", m.InAn ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        private static long InsertCaiDatCDBoc(SQLiteConnection conn, SQLiteTransaction tx, long tpId, CaiDatCDBoc m)
        {
            const string sql = @"
            INSERT INTO CaiDatCDBoc
            (TTThanhPham_ID, MangNuoc, PuliDanDay, BoDemMet, MayIn,
             v1, v2, v3, v4, v5, v6, Co, Dau1, Dau2, Khuon, BinhSay,
             DKKhuon1, DKKhuon2, TTNhua, NhuaPhe, GhiChuNhuaPhe, DayPhe, GhiChuDayPhe,
             KTDKLan1, KTDKLan2, KTDKLan3, DiemMongLan1, DiemMongLan2)
            VALUES
            (@TTThanhPham_ID, @MangNuoc, @PuliDanDay, @BoDemMet, @MayIn,
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

            cmd.Parameters.AddWithValue("@DKKhuon1", m.DKKhuon1);
            cmd.Parameters.AddWithValue("@DKKhuon2", m.DKKhuon2);
            cmd.Parameters.AddWithValue("@TTNhua", m.TTNhua ?? string.Empty);
            cmd.Parameters.AddWithValue("@NhuaPhe", m.NhuaPhe);
            cmd.Parameters.AddWithValue("@GhiChuNhuaPhe", (object?)m.GhiChuNhuaPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DayPhe", m.DayPhe);
            cmd.Parameters.AddWithValue("@GhiChuDayPhe", (object?)m.GhiChuDayPhe ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@KTDKLan1", m.KTDKLan1);
            cmd.Parameters.AddWithValue("@KTDKLan2", m.KTDKLan2);
            cmd.Parameters.AddWithValue("@KTDKLan3", m.KTDKLan3);
            cmd.Parameters.AddWithValue("@DiemMongLan1", m.DiemMongLan1);
            cmd.Parameters.AddWithValue("@DiemMongLan2", m.DiemMongLan2);

            try
            {
                cmd.ExecuteNonQuery();
                return conn.LastInsertRowId;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm dữ liệu vào bảng CaiDatCDBoc.", ex);
            }

        }

        private static void InsertCDBocMach(SQLiteConnection conn, SQLiteTransaction tx, long id, CD_BocMach m)
        {
            const string sql = @"
            INSERT INTO CD_BocMach (CaiDatCDBoc_ID, NgoaiQuan, LanDanhThung, SoMet)
            VALUES (@CaiDatCDBoc_ID, @NgoaiQuan, @LanDanhThung, @SoMet);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@CaiDatCDBoc_ID", id);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? "1"); // default theo schema
            cmd.Parameters.AddWithValue("@LanDanhThung", m.LanDanhThung);
            cmd.Parameters.AddWithValue("@SoMet", m.SoMet);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDKeoRut(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_KeoRut m)
        {
            const string sql = @"
            INSERT INTO CD_KeoRut
            (TTThanhPham_ID, DKTrucX, DKTrucY, NgoaiQuan, TocDo, DienApU, DongDienU)
            VALUES
            (@TTThanhPham_ID, @DKTrucX, @DKTrucY, @NgoaiQuan, @TocDo, @DienApU, @DongDienU);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKTrucX", m.DKTrucX);
            cmd.Parameters.AddWithValue("@DKTrucY", m.DKTrucY);
            cmd.Parameters.AddWithValue("@NgoaiQuan", m.NgoaiQuan ?? string.Empty);
            cmd.Parameters.AddWithValue("@TocDo", m.TocDo);
            cmd.Parameters.AddWithValue("@DienApU", m.DienApU);
            cmd.Parameters.AddWithValue("@DongDienU", m.DongDienU);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDBenRuot(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_BenRuot m)
        {
            // Lưu ý cột "Chiều Xoắn" có dấu và khoảng trắng -> cần trích dẫn bằng dấu "
            const string sql = @"
            INSERT INTO CD_BenRuot
            (TTThanhPham_ID, DKSoi, SoSoi, ChieuXoan, BuocBen)
            VALUES
            (@TTThanhPham_ID, @DKSoi, @SoSoi, @ChieuXoan, @BuocBen);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@DKSoi", m.DKSoi);
            cmd.Parameters.AddWithValue("@SoSoi", (object?)m.SoSoi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@BuocBen", m.BuocBen);
            cmd.ExecuteNonQuery();
        }

        private static void InsertCDGhepLoiQB(SQLiteConnection conn, SQLiteTransaction tx, long thongTinSpId, CD_GhepLoiQB m)
        {
            const string sql = @"
            INSERT INTO CD_GhepLoiQB
            (TTThanhPham_ID, BuocXoan, ChieuXoan, GoiCachMep, DKBTP)
            VALUES
            (@TTThanhPham_ID, @BuocXoan, @ChieuXoan, @GoiCachMep, @DKBTP);";

            using var cmd = new SQLiteCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@TTThanhPham_ID", thongTinSpId);
            cmd.Parameters.AddWithValue("@BuocXoan", m.BuocXoan);
            cmd.Parameters.AddWithValue("@ChieuXoan", m.ChieuXoan ?? "Z");
            cmd.Parameters.AddWithValue("@GoiCachMep", m.GoiCachMep);
            cmd.Parameters.AddWithValue("@DKBTP", m.DKBTP);
            cmd.ExecuteNonQuery();
        }

        public static string InsertDSMaSP(DanhSachMaSP sp)
        {
            try
            {
                using var conn = new SQLiteConnection(_connStr);
                conn.Open();
                using var tx = conn.BeginTransaction();

                string sql = @"
                    INSERT INTO DanhSachMaSP (Ten, Ma, DonVi, KieuSP, DateInsert)
                    VALUES (@Ten, @Ma, @DonVi, @KieuSP, @DateInsert);
                ";

                using (var cmd = new SQLiteCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Ten", sp.Ten);
                    cmd.Parameters.AddWithValue("@Ma", sp.Ma);
                    cmd.Parameters.AddWithValue("@DonVi", sp.DonVi);
                    cmd.Parameters.AddWithValue("@KieuSP", sp.KieuSP);
                    cmd.Parameters.AddWithValue("@DateInsert", sp.DateInsert ?? DateTime.Now);

                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return ""; 
            }
            catch (Exception ex)
            {
                return Helper.Helper.ShowErrorDatabase(ex, sp.Ma);
            }
        }
        #endregion


        #region setup config
        public static void UpdateConfig(ConfigDB config)
        {
            string query = "UPDATE ConfigDB SET Active = @active, Message = @message WHERE ID = @id";
            string mess = "CẬP NHẬT THÀNH CÔNG";
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(_connStr))
                {
                    conn.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@active", config.Active);
                        cmd.Parameters.AddWithValue("@message", config.Message);
                        cmd.Parameters.AddWithValue("@id", config.ID);

                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }

            }
            catch (Exception)
            {
                mess = "CẬP NHẬT THẤT BẠI";
            }

            MessageBox.Show(mess, "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
        #endregion

        #region ===================================
        private static void insertVersion1(TTThanhPham tp,ThongTinCaLamViec tt, List<TTNVL> nvlList)
        {
            TonKho tonKho = new TonKho
            {
                Lot = tp.MaBin,
                MaSP_ID = tp.DanhSachSP_ID,
                KhoiLuongDauVao = (decimal)tp.KhoiLuongTruoc,
                KhoiLuongConLai = (decimal)tp.KhoiLuongSau,
                HanNoi = 0,
                ChieuDai = (decimal)tp.ChieuDaiTruoc
            };

            DL_CD_Ben dL_CD_Ben = new DL_CD_Ben
            {
                Ngay = tt.Ngay,
                Ca = tt.Ca,
                NguoiLam = tt.NguoiLam,
                SoMay = tt.May,
                GhiChu = tp.GhiChu,
            };
            DatabasehelperVer01.InsertSanPhamTonKhoDL<TonKho, DL_CD_Ben>(tonKho, dL_CD_Ben, "dL_CD_Ben", nvlList);
        }

        private static void updateVersion1(int id, TTThanhPham tp, ThongTinCaLamViec tt)
        {
            TonKho tonKho = new TonKho
            {
                Lot = tp.MaBin,
                MaSP_ID = tp.DanhSachSP_ID,
                KhoiLuongDauVao = (decimal)tp.KhoiLuongTruoc,
                KhoiLuongConLai = (decimal)tp.KhoiLuongSau,
                HanNoi = 0,
                ChieuDai = (decimal)tp.ChieuDaiTruoc
            };
            DL_CD_Ben dL_CD_Ben = new DL_CD_Ben
            {
                Ngay = tt.Ngay,
                Ca = tt.Ca,
                NguoiLam = tt.NguoiLam,
                SoMay = tt.May,
                GhiChu = tp.GhiChu,
            };
            DatabasehelperVer01.UpdateDL_CDBen(id, tonKho, dL_CD_Ben);
        }

        #endregion

    }


}
