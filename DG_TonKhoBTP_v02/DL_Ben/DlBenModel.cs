using DG_TonKhoBTP_v02.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.DL_Ben
{
    public class TonKho
    {
        [AutoIncrement]
        public int ID { get; set; }
        public string Lot { get; set; }
        public int MaSP_ID { get; set; }
        public decimal KhoiLuongDauVao { get; set; }
        public decimal KhoiLuongConLai { get; set; }
        public int HanNoi { get; set; }
        public decimal ChieuDai { get; set; }
    }

    public class DL_CD_Ben
    {
        public string Ngay { get; set; }

        [Required(ErrorMessage = "Ca không được để trống")]
        public string Ca { get; set; }

        [Required(ErrorMessage = "TonKho_ID không được để trống")]
        public int TonKho_ID { get; set; }

        [Required(ErrorMessage = "Người làm không được để trống")]
        public string NguoiLam { get; set; }

        [Required(ErrorMessage = "Số máy không được để trống")]
        public string SoMay { get; set; }
        public string GhiChu { get; set; }
        public string DateInsert { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public decimal KLHanNoi { get; set; } = 0;
    }

    public static class DatabasehelperVer01
    {
        public static string _connStr;
        public static string _connStr2;

        // Thiết lập đường dẫn đến cơ sở dữ liệu SQLite
        public static void SetDatabasePath(string path)
        {
            // database version 2
            _connStr2 = $"Data Source={path};Version=3;"; 

            string newPath = path.Replace("QLSX_v02", "QLSX_DG_New");
            // database version 1
            _connStr = $"Data Source={newPath};Version=3;";
        }

        // Cập nhật db
        public static Boolean UpdateDL_CDBen(int id, TonKho tonKho, DL_CD_Ben ben)
        {
            string maBin = "";
            using (var conn = new SQLiteConnection(_connStr2))
            {
                conn.Open();
                maBin = GetMaBinFromID(conn, id);
                conn.Close();
            }

            Boolean result = true;
            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Cập nhật bảng TonKho
                        result = UpdateTonKho(conn,tonKho, maBin);

                        // Cập nhật bảng DL_CD_Ben
                        if (result) result = UpdateDL_CDBen_ByMaBin_OneQuery(conn,ben, maBin);

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        MessageBox.Show("Lỗi: " + ex.Message, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        result = false;
                    }
                }
            }
            return result;
        }

        public static bool UpdateTonKho(SQLiteConnection conn, TonKho tk, string lot)
        {
                string sql = @"
                    UPDATE TonKho SET
                        MaSP_ID = @MaSP_ID,
                        KhoiLuongDauVao = @KhoiLuongDauVao,
                        KhoiLuongConLai = @KhoiLuongConLai,
                        ChieuDai = @ChieuDai
                    WHERE Lot = @Lot;
                ";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MaSP_ID", tk.MaSP_ID);
                    cmd.Parameters.AddWithValue("@KhoiLuongDauVao", tk.KhoiLuongDauVao);
                    cmd.Parameters.AddWithValue("@KhoiLuongConLai", tk.KhoiLuongConLai);
                    cmd.Parameters.AddWithValue("@ChieuDai", tk.ChieuDai);

                    cmd.Parameters.AddWithValue("@Lot", lot);

                    return cmd.ExecuteNonQuery() > 0;
                }
        }

        public static bool UpdateDL_CDBen_ByMaBin_OneQuery(SQLiteConnection conn, DL_CD_Ben ben, string mabin)
        {            

            string sql = @"
                UPDATE DL_CD_Ben
                SET 
                    Ngay       = @ngay,
                    Ca         = @ca,
                    NguoiLam   = @nguoilam,
                    SoMay      = @somay,
                    GhiChu     = @ghichu
                WHERE TonKho_ID = (
                    SELECT ID FROM TonKho WHERE Lot = @lot LIMIT 1
                );
            ";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                // Map theo cấu trúc bảng DL_CD_Ben
                cmd.Parameters.AddWithValue("@ngay", ben.Ngay);
                cmd.Parameters.AddWithValue("@ca", ben.Ca);
                cmd.Parameters.AddWithValue("@nguoilam", ben.NguoiLam);
                cmd.Parameters.AddWithValue("@somay", ben.SoMay);
                cmd.Parameters.AddWithValue("@ghichu", (object?)ben.GhiChu ?? "");

                // Giả sử ben.Lot là mã Bin / Lot để join với TonKho
                cmd.Parameters.AddWithValue("@lot", mabin);

                int affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
        }

        private static string GetMaBinFromID(SQLiteConnection connection, int id)
        {
            using (var command = new SQLiteCommand("SELECT Mabin FROM TTThanhPham WHERE ID = @ID", connection))
            {
                command.Parameters.AddWithValue("@ID", id);
                var result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    throw new Exception("TonKho_ID not found for DL_CD_\"+ table +\".ID = " + id);

                return (string)result;
            }
        }

        // Tạo mới db
        public static void InsertSanPhamTonKhoDL<TTonKho, TDLCongDoan>(TTonKho tonKho, TDLCongDoan dlModel, string table, List<TTNVL> nvlList)
        where TTonKho : class
        where TDLCongDoan : class
        {
            using (var connection = new SQLiteConnection(_connStr))
            {
                connection.Open();
                using (var tran = connection.BeginTransaction())
                {                    
                    try
                    {
                        int tonKho_ID = InsertModelToDatabase(tonKho, "TonKho", connection, tran);
                        
                        UpdateTonKho_NVL(nvlList, tonKho_ID, connection);

                        // Gán thuộc tính TonKho_ID bằng reflection hoặc dynamic
                        dynamic dynamicModel = dlModel;
                        dynamicModel.TonKho_ID = tonKho_ID;

                        InsertModelToDatabase(dynamicModel, table, connection, tran);

                        tran.Commit();
                    }
                    catch (SQLiteException ex)
                    {
                        tran.Rollback();
                        if (ex.ResultCode == SQLiteErrorCode.Constraint && ex.Message.Contains("UNIQUE"))
                        {
                            MessageBox.Show("LOT vừa nhập đã tồn tại",
                                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("Có lỗi khi lưu dữ liệu. Chi tiết: " + ex.Message,
                                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                       connection.Close();
                    }
                }

            }
        }

        public static int InsertModelToDatabase<T>(T model, string tableName, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Where(p => !Attribute.IsDefined(p, typeof(AutoIncrementAttribute))) // Bỏ qua AutoIncrement
                .ToList();

            string columns = string.Join(", ", properties.Select(p => p.Name));
            string parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            string query = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (var command = new SQLiteCommand(query, connection, transaction))
            {
                foreach (var prop in properties)
                {
                    object value = prop.GetValue(model) ?? DBNull.Value;
                    command.Parameters.AddWithValue($"@{prop.Name}", value);
                }

                command.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand("SELECT last_insert_rowid();", connection, transaction))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateTonKho_NVL_Lan1(List<TTNVL> nvlList)
        {
            string sql = @"UPDATE TonKho
                       SET KhoiLuongConLai = @KhoiLuongConLai,
                           ChieuDai = @ChieuDai
                       WHERE Lot = @Lot";
            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    // Chuẩn bị sẵn param (đỡ phải Add mỗi vòng loop)
                    cmd.Parameters.Add("@KhoiLuongConLai", System.Data.DbType.Double);
                    cmd.Parameters.Add("@ChieuDai", System.Data.DbType.Double);
                    cmd.Parameters.Add("@Lot", System.Data.DbType.String);

                    foreach (var nvl in nvlList)
                    {
                        if (string.IsNullOrWhiteSpace(nvl.BinNVL))
                            continue; // Không có BinNVL thì bỏ qua

                        cmd.Parameters["@KhoiLuongConLai"].Value = nvl.KlConLai ?? 0;
                        cmd.Parameters["@ChieuDai"].Value = nvl.CdConLai ?? 0;
                        cmd.Parameters["@Lot"].Value = nvl.BinNVL;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void UpdateTonKho_NVL_Lan2(List<TTNVL> nvlList, string maBin)
        {
            string sql = @"
                UPDATE TonKho
                SET KhoiLuongConLai = @KhoiLuongConLai,
                    ChieuDai        = @ChieuDai
                WHERE Lot = @Lot
                  AND ID_Cuoi = (
                        SELECT t2.ID
                        FROM TonKho AS t2
                        WHERE t2.Lot = @MaBin
                        LIMIT 1
                  );";

            using (var conn = new SQLiteConnection(_connStr))
            {
                conn.Open();

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    // chuẩn bị param cố định
                    cmd.Parameters.Add("@KhoiLuongConLai", System.Data.DbType.Double);
                    cmd.Parameters.Add("@ChieuDai", System.Data.DbType.Double);
                    cmd.Parameters.Add("@Lot", System.Data.DbType.String);
                    cmd.Parameters.Add("@MaBin", System.Data.DbType.String).Value = maBin;

                    foreach (var nvl in nvlList)
                    {
                        if (string.IsNullOrWhiteSpace(nvl.BinNVL))
                            continue;

                        cmd.Parameters["@KhoiLuongConLai"].Value = nvl.KlConLai ?? 0;
                        cmd.Parameters["@ChieuDai"].Value = nvl.CdConLai ?? 0;
                        cmd.Parameters["@Lot"].Value = nvl.BinNVL;   // Lot cần update

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        public static void UpdateTonKho_NVL(List<TTNVL> nvlList, int tonKho_ID, SQLiteConnection conn)
        {
            string sql = @"UPDATE TonKho
                       SET  ID_Cuoi = @ID_Cuoi
                       WHERE Lot = @Lot";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.Add("@ID_Cuoi", System.Data.DbType.Int32);
                cmd.Parameters.Add("@Lot", System.Data.DbType.String);

                foreach (var nvl in nvlList)
                {
                    if (string.IsNullOrWhiteSpace(nvl.BinNVL))
                        continue; // Không có BinNVL thì bỏ qua
                    cmd.Parameters["@ID_Cuoi"].Value = tonKho_ID;
                    cmd.Parameters["@Lot"].Value = nvl.BinNVL;

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
