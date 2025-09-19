
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace DG_TonKhoBTP_v02.Data
{

    public static class DatabaseHelper
    {
        private static string connStr;

        //==========================================================
        // Thiết lập đường dẫn đến cơ sở dữ liệu SQLite
        public static void SetDatabasePath(string path)
        {
            connStr = $"Data Source={path};Version=3;";
        }

        #region
        /*

        // Kiểm tra và thêm dữ liệu vào DanhSachMaSP nếu chưa tồn tại
        public static Boolean InsertSP(string ma, string ten)
        {

            string typeProduct = KtraMaSP(ma);

            if (typeProduct == "") return false;

            string insertQuery = "INSERT INTO DanhSachMaSP" +
                         " (Ma, Ten, KieuSP ,DateInsert) VALUES (@ma, @ten, @typeProduct, strftime('%Y-%m-%d', 'now', 'localtime'));";
            try

            {
                using (SQLiteConnection conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ma", ma);
                        cmd.Parameters.AddWithValue("@ten", ten);
                        cmd.Parameters.AddWithValue("@typeProduct", typeProduct);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Đã thêm sản phẩm thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Lỗi khi ghi vào database:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Thực hiện chèn dữ liệu vào bảng DL_CongDoan và bảng TonKho
        public static Boolean InsertSanPhamTonKhoDL<TTonKho, TDLCongDoan>(TTonKho tonKho, TDLCongDoan dlModel, string table)
        where TTonKho : class
        where TDLCongDoan : class
        {
            using (var connection = new SQLiteConnection(connStr))
            {
                connection.Open();
                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        InsertModelToDatabase(tonKho, "TonKho", connection, tran);
                        int tonKho_ID = (int)(long)new SQLiteCommand("SELECT last_insert_rowid()", connection, tran).ExecuteScalar();

                        // Gán thuộc tính TonKho_ID bằng reflection hoặc dynamic
                        dynamic dynamicModel = dlModel;
                        dynamicModel.TonKho_ID = tonKho_ID;

                        InsertModelToDatabase(dynamicModel, table, connection, tran);

                        tran.Commit();
                        return true;
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
                        return false;
                    }
                }
            }

        }

        // Thêm dữ liệu vào bảng với tên bảng và mô hình đã cho
        public static void InsertModelToDatabase<T>(T model, string tableName, SQLiteConnection connection, SQLiteTransaction transaction)
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
        }

        public static bool InsertVaUpdateTonKho_GopLot(TonKho tonKhoNew, DL_CD_Ben hanNoiNew, IEnumerable<long> ids)
        {
            if (tonKhoNew == null) throw new ArgumentNullException(nameof(tonKhoNew));
            if (hanNoiNew == null) throw new ArgumentNullException(nameof(hanNoiNew));
            if (string.IsNullOrWhiteSpace(tonKhoNew.Lot))
                throw new ArgumentException("tonKhoNew.Lot bắt buộc (NOT NULL, UNIQUE).");

            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        long newTonKhoId;

                        // 1) Insert TonKho
                        using (var cmd = new SQLiteCommand(@"
                            INSERT INTO TonKho
                              (Lot, MaSP_ID, KhoiLuongDauVao, KhoiLuongConLai, HanNoi, ChieuDai)
                            VALUES
                              (@Lot, @MaSP_ID, @KhoiLuongDauVao, @KhoiLuongConLai, @HanNoi, @ChieuDai);
                        ", conn, tran))
                        {
                            cmd.Parameters.Add("@Lot", DbType.String).Value = tonKhoNew.Lot;
                            cmd.Parameters.Add("@MaSP_ID", DbType.Int32).Value = tonKhoNew.MaSP_ID;
                            cmd.Parameters.Add("@KhoiLuongDauVao", DbType.Double).Value = tonKhoNew.KhoiLuongDauVao;
                            cmd.Parameters.Add("@KhoiLuongConLai", DbType.Double).Value = tonKhoNew.KhoiLuongConLai;
                            cmd.Parameters.Add("@HanNoi", DbType.Int32).Value = tonKhoNew.HanNoi;
                            cmd.Parameters.Add("@ChieuDai", DbType.Double).Value = tonKhoNew.ChieuDai;

                            cmd.ExecuteNonQuery();
                            newTonKhoId = conn.LastInsertRowId;

                            // 2) Insert DL_CD_Ben
                            using (var cmd2 = new SQLiteCommand(@"
                                INSERT INTO DL_CD_Ben
                                  (Ngay, Ca,TonKho_ID, NguoiLam, SoMay, GhiChu)
                                VALUES
                                  (@Ngay, @Ca,@TonKho_ID, @NguoiLam, @SoMay, @GhiChu);
                            ", conn, tran))
                            {
                                cmd2.Parameters.Add("@Ngay", DbType.String).Value = hanNoiNew.Ngay;
                                cmd2.Parameters.Add("@Ca", DbType.String).Value = hanNoiNew.Ca;
                                cmd2.Parameters.Add("@TonKho_ID", DbType.Int64).Value = newTonKhoId;
                                cmd2.Parameters.Add("@NguoiLam", DbType.String).Value = hanNoiNew.NguoiLam;
                                cmd2.Parameters.Add("@SoMay", DbType.String).Value = hanNoiNew.SoMay;
                                cmd2.Parameters.Add("@GhiChu", DbType.String).Value = hanNoiNew.GhiChu;
                                cmd2.ExecuteNonQuery();
                            }
                        }


                        // 3) Update TonKho.KhoiLuongConLai = 0 cho danh sách ids
                        var idList = (ids ?? Enumerable.Empty<long>()).Distinct().ToList();
                        if (idList.Count > 0)
                        {
                            var paramNames = idList.Select((_, i) => $"@id{i}").ToArray();
                            var sqlUpdate = $"UPDATE TonKho SET KhoiLuongConLai = 0, HanNoi ={newTonKhoId} WHERE ID IN ({string.Join(",", paramNames)})";

                            using (var cmd3 = new SQLiteCommand(sqlUpdate, conn, tran))
                            {
                                for (int i = 0; i < idList.Count; i++)
                                    cmd3.Parameters.Add(paramNames[i], DbType.Int64).Value = idList[i];
                                cmd3.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        //=================================================================
        // Cập nhật dữ liệu trong bảng DL_CongDoan và cập nhật lượng TonKho theo ID
        public static Boolean UpdateDL_CDBoc(int bocID, TonKho tonKho, DL_CD_Boc dl)
        {
            Boolean result = false;
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Lấy TonKho_ID từ bảng DL_CD_Boc
                        int tonKhoID = GetTonKhoIDFromID(conn, bocID,"boc");

                        // Cập nhật bảng TonKho
                        result = UpdateModelInDatabase(tonKho, "TonKho", "ID", tonKhoID, conn, tran);
                        dl.TonKho_ID = tonKhoID;

                        // Cập nhật bảng DL_CD_Boc
                        if (result) result = UpdateModelInDatabase(dl, "DL_CD_Boc", "ID", bocID, conn, tran);

                        tran.Commit();
                    }
                    catch(Exception ex) 
                    {
                        tran.Rollback();
                        MessageBox.Show("Lỗi: " + ex.Message, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        result = false;
                    }
                }
            }
            return result;
        }

        // Cập nhật dữ liệu trong bảng DL_CD_Ben và cập nhật lượng TonKho theo ID
        public static Boolean UpdateDL_CDBen( int benID, TonKho tonKho, DL_CD_Ben ben)
        {
            Boolean result = true;
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Lấy TonKho_ID từ bảng DL_CD_Ben
                        int tonKhoID = GetTonKhoIDFromID(conn, benID);

                        // Cập nhật bảng TonKho
                        result = UpdateModelInDatabase(tonKho, "TonKho", "ID", tonKhoID, conn, tran);

                        ben.TonKho_ID = tonKhoID;

                        // Cập nhật bảng DL_CD_Ben
                        if(result) result = UpdateModelInDatabase(ben, "DL_CD_Ben", "ID", benID, conn, tran);
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

        // Cập nhật dữ liệu trong bảng với tên bảng và mô hình đã cho
        private static Boolean UpdateModelInDatabase<T>(T model, string tableName, string keyColumn, object keyValue, SQLiteConnection connection, SQLiteTransaction transaction)
        {
            try
            {
                var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.Name != keyColumn) // Bỏ qua cột khóa chính
                .ToList();

                string setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                string query = $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @KeyValue";

                using (var command = new SQLiteCommand(query, connection, transaction))
                {
                    foreach (var prop in properties)
                    {
                        object value = prop.GetValue(model) ?? DBNull.Value;
                        command.Parameters.AddWithValue($"@{prop.Name}", value);
                    }

                    command.Parameters.AddWithValue("@KeyValue", keyValue);
                    command.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }            
        }

        // Cập nhật số lượng còn lại thực tế trong bảng TonKho
        public static Boolean UpdateTonKho_SLConLaiThucTe(TonKho tk)
        {
            Boolean result = true;
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                string query = @"
                UPDATE TonKho 
                SET KhoiLuongConLai = @KhoiLuongMoi 
                WHERE Lot = @Lot";

                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@KhoiLuongMoi", tk.KhoiLuongConLai);
                    cmd.Parameters.AddWithValue("@Lot", tk.Lot);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        


        public static bool UpdateKhoiLuongConLai<TTonKho, TDLCongDoan>(
        TTonKho tonKho,
        TDLCongDoan dlModel,
        string dlTable,
        IEnumerable<TonKho> tonKhoUpdates = null)
        where TTonKho : class
        where TDLCongDoan : class
        {
            if (tonKho == null) throw new ArgumentNullException(nameof(tonKho));
            if (dlModel == null) throw new ArgumentNullException(nameof(dlModel));
            if (string.IsNullOrWhiteSpace(dlTable)) throw new ArgumentNullException(nameof(dlTable));

            using (var connection = new SQLiteConnection(connStr))
            {
                connection.Open();

                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        // 1) Insert TonKho
                        InsertModelToDatabase(tonKho, "TonKho", connection, tran);

                        // Lấy ID vừa insert
                        int tonKho_ID = (int)(long)new SQLiteCommand("SELECT last_insert_rowid()", connection, tran)
                            .ExecuteScalar();

                        // 2) Gán TonKho_ID vào model DL (ưu tiên reflection an toàn)
                        var prop = typeof(TDLCongDoan).GetProperty("TonKho_ID");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(dlModel, tonKho_ID);
                        }
                        else
                        {
                            // fallback dynamic nếu class là Expando/dynamic
                            try
                            {
                                dynamic d = dlModel;
                                d.TonKho_ID = tonKho_ID;
                            }
                            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                            {
                                throw new InvalidOperationException(
                                    $"Model {typeof(TDLCongDoan).Name} không có thuộc tính TonKho_ID để gán khoá ngoại.");
                            }
                        }

                        // 3) Insert bản ghi DL vào bảng được chỉ định
                        InsertModelToDatabase(dlModel, dlTable, connection, tran);

                        // 4) (Tuỳ chọn) Cập nhật KhoiLuongConLai cho danh sách TonKho khác
                        if (tonKhoUpdates != null)
                        {
                            using (var cmd = new SQLiteCommand(
                                "UPDATE TonKho SET KhoiLuongConLai = @KhoiLuongConLai, HanNoi = @HanNoi WHERE ID = @ID;", connection, tran))
                            {
                                var pValue = cmd.Parameters.Add("@KhoiLuongConLai", DbType.Decimal);
                                var pId = cmd.Parameters.Add("@ID", DbType.Int32);
                                var pHanNoi = cmd.Parameters.Add("@HanNoi", DbType.Int32);

                                foreach (var item in tonKhoUpdates)
                                {
                                    if (item == null) continue;

                                    pValue.Value = item.KhoiLuongConLai;
                                    pId.Value = item.ID;
                                    pHanNoi.Value = tonKho_ID; // giá trị lấy từ insert ở bước đầu

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // 5) Commit tất cả
                        tran.Commit();
                        return true;
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
                        return false;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        MessageBox.Show("Có lỗi khi lưu dữ liệu. Chi tiết: " + ex.Message,
                                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
        }


        // Cập nhật một bảng với câu lệnh SQL và tham số
        public static bool UpdateATable(string sql, Dictionary<string, object> parameters)
        {
            int affectedRows = 0;

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connStr))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);

                        conn.Open();
                        affectedRows = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message);
            }

            return affectedRows != 0;
        }
                
        public static bool UpdateKhoiLuongVaBin(string id, decimal khoiLuongDauVao, string tenBin, decimal khoiLuongBin)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                using (SQLiteTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Cập nhật TonKho
                        string updateTonKhoQuery = @"
                                UPDATE TonKho 
                                SET KhoiLuongDauVao = @KhoiLuongDauVao, 
                                    KhoiLuongConLai = @KhoiLuongDauVao 
                                WHERE ID = @ID";

                        using (SQLiteCommand cmd1 = new SQLiteCommand(updateTonKhoQuery, conn, transaction))
                        {
                            cmd1.Parameters.AddWithValue("@KhoiLuongDauVao", khoiLuongDauVao);
                            cmd1.Parameters.AddWithValue("@ID", id);
                            int rowsAffected = cmd1.ExecuteNonQuery();
                            if (rowsAffected == 0)
                            {
                                throw new Exception("Không tìm thấy bản ghi trong cơ sở dữ liệu để cập nhật.");
                            }
                        }

                        // Cập nhật hoặc thêm mới vào DanhSachBin
                        string upsertBinQuery = @"
                            INSERT INTO DanhSachBin (TenBin, KhoiLuongBin)
                            VALUES (@TenBin, @KhoiLuongBin)
                            ON CONFLICT(TenBin)
                            DO UPDATE SET KhoiLuongBin = excluded.KhoiLuongBin;
                        ";
                        using (SQLiteCommand cmd2 = new SQLiteCommand(upsertBinQuery, conn, transaction))
                        {
                            cmd2.Parameters.AddWithValue("@TenBin", tenBin);
                            cmd2.Parameters.AddWithValue("@KhoiLuongBin", khoiLuongBin);
                            cmd2.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Lỗi: " + ex.Message, "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
        }

        public static bool Update_Mica(
            long sttB,
            TonKho tonKhoMoi,
            DL_CD_Boc dL_CD_Boc,
            List<TonKho> tonKho_update)
        {
            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        // 1) Update TonKho(ID=sttB)
                        using (var cmd1 = conn.CreateCommand())
                        {
                            cmd1.Transaction = tran;
                            cmd1.CommandText = @"
                        UPDATE TonKho
                        SET KhoiLuongConLai = COALESCE(@klcl, KhoiLuongConLai),
                            HanNoi          = @hanNoi,
                            MaSP_ID         = COALESCE(@masp, MaSP_ID),
                            ChieuDai        = COALESCE(@cd, ChieuDai),
                            KhoiLuongDauVao = COALESCE(@kldv, KhoiLuongDauVao),
                            Lot             = COALESCE(@lot, Lot)
                        WHERE ID=@id;";
                            cmd1.Parameters.AddWithValue("@klcl", tonKhoMoi.KhoiLuongConLai);
                            cmd1.Parameters.AddWithValue("@hanNoi", 0);
                            cmd1.Parameters.AddWithValue("@masp", tonKhoMoi.MaSP_ID);
                            cmd1.Parameters.AddWithValue("@cd", tonKhoMoi.ChieuDai);
                            cmd1.Parameters.AddWithValue("@kldv", tonKhoMoi.KhoiLuongDauVao);
                            cmd1.Parameters.AddWithValue("@lot", tonKhoMoi.Lot);
                            cmd1.Parameters.AddWithValue("@id", sttB);
                            cmd1.ExecuteNonQuery();
                        }

                        // 2) Update DL_CD_Boc(TonKho_ID=sttB)
                        using (var cmd2 = conn.CreateCommand())
                        {
                            cmd2.Transaction = tran;
                            cmd2.CommandText = @"
                        UPDATE DL_CD_Boc
                        SET Ngay              = COALESCE(@Ngay, Ngay),
                            Ca                = COALESCE(@Ca, Ca),
                            KhoiLuongPhe      = COALESCE(@KLP, KhoiLuongPhe),
                            NguoiLam          = COALESCE(@NL, NguoiLam),
                            SoMay             = COALESCE(@SM, SoMay),
                            GhiChu            = COALESCE(@GC, GhiChu),
                            MaSP_ID           = COALESCE(@MaSP, MaSP_ID),
                            CD_Ben_ID         = COALESCE(@CDB, CD_Ben_ID),
                            KhoiLuongTruocBoc = COALESCE(@KLTB, KhoiLuongTruocBoc),
                            KhoiLuongConLai   = COALESCE(@KLCLB, KhoiLuongConLai),
                            TenCongDoan       = COALESCE(@TCD, TenCongDoan)
                        WHERE TonKho_ID=@tkid;";
                            cmd2.Parameters.AddWithValue("@Ngay", dL_CD_Boc.Ngay);
                            cmd2.Parameters.AddWithValue("@Ca", dL_CD_Boc.Ca);
                            cmd2.Parameters.AddWithValue("@KLP", dL_CD_Boc.KhoiLuongPhe);
                            cmd2.Parameters.AddWithValue("@NL", dL_CD_Boc.NguoiLam);
                            cmd2.Parameters.AddWithValue("@SM", dL_CD_Boc.SoMay);
                            cmd2.Parameters.AddWithValue("@GC", dL_CD_Boc.GhiChu);
                            cmd2.Parameters.AddWithValue("@MaSP", dL_CD_Boc.MaSP_ID);
                            cmd2.Parameters.AddWithValue("@CDB", dL_CD_Boc.CD_Ben_ID);
                            cmd2.Parameters.AddWithValue("@KLTB", dL_CD_Boc.KhoiLuongTruocBoc);
                            cmd2.Parameters.AddWithValue("@KLCLB", dL_CD_Boc.KhoiLuongConLai);
                            cmd2.Parameters.AddWithValue("@TCD", dL_CD_Boc.TenCongDoan);
                            cmd2.Parameters.AddWithValue("@tkid", sttB);
                            cmd2.ExecuteNonQuery();
                        }

                        // 3) Reset HanNoi cũ
                        using (var cmd3 = conn.CreateCommand())
                        {
                            cmd3.Transaction = tran;
                            cmd3.CommandText = "UPDATE TonKho SET HanNoi=NULL WHERE HanNoi=@sttB;";
                            cmd3.Parameters.AddWithValue("@sttB", sttB);
                            cmd3.ExecuteNonQuery();
                        }

                        // 4) Update danh sách TonKho_update
                        if (tonKho_update != null && tonKho_update.Count > 0)
                        {
                            using (var cmd4 = conn.CreateCommand())
                            {
                                cmd4.Transaction = tran;
                                cmd4.CommandText = @"
                                    UPDATE TonKho
                                    SET KhoiLuongConLai=@kl, HanNoi=@h
                                    WHERE ID=@id;";
                                var pKl = cmd4.Parameters.Add("@kl", DbType.Double);
                                var pH = cmd4.Parameters.Add("@h", DbType.Int64);
                                var pId = cmd4.Parameters.Add("@id", DbType.Int64);

                                foreach (var item in tonKho_update)
                                {
                                    pKl.Value = item.KhoiLuongConLai;
                                    pH.Value = sttB;
                                    pId.Value = item.ID;
                                    cmd4.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        */
        #endregion

        // =================================================================


        // Lấy dữ liệu từ bảng theo  1 khoá - key
        public static DataTable GetData(string key, string query, string para)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@" + para, key);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        #region
        /*

        // Lấy TonKho_ID từ ID trong bảng DL_CD_Boc hoặc DL_CD_Ben

        private static int GetTonKhoIDFromID(SQLiteConnection connection, int id, string table = "Ben")
        {
            using (var command = new SQLiteCommand("SELECT TonKho_ID FROM DL_CD_"+ table +" WHERE ID = @ID", connection))
            {
                command.Parameters.AddWithValue("@ID", id);
                var result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    throw new Exception("TonKho_ID not found for DL_CD_\"+ table +\".ID = " + id);

                return Convert.ToInt32(result);
            }
        }

        // Lấy dữ liệu từ bảng theo ngày
        public static DataTable GetDataByDate(string ngay, string query)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Ngay", ngay);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        // Lấy dữ liệu từ bảng DL_CD_Boc theo ID
        public static DataTable GetDL_CDBenByID(int id, string query)
        {
            DataTable resultTable = new DataTable();

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(resultTable);
                    }
                }
            }

            return resultTable;
        }

        // Lấy tên sản phẩm và mã từ bảng DanhSachMaSP
        public static List<ProductModel> GetProductNamesAndPartNumber(string query, string keyword)
        {
            List<ProductModel> results = new List<ProductModel>();
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", keyword);

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["id"]);
                                string ma = reader["ma"].ToString();
                                string ten = reader["ten"].ToString();
                                results.Add(new ProductModel(id, ma, ten));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi SQLite: " + ex.Message);
            }

            return results;
        }

        // Lấy dữ liệu của bảng dựa trên câu truy vấn
        public static DataTable GetDataFromSQL( string query)
        {

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable resultTable = new DataTable();
                        adapter.Fill(resultTable);
                        return resultTable;
                    }
                }
            }
        }

        // =================================================================
            
        public static string KtraMaSP(string ma)
        {
            int dotIndex = ma.IndexOf('.');

            string typeProduct = dotIndex >= 0 ? ma.Substring(0, dotIndex) : ma;
            typeProduct = typeProduct.ToUpper();

            if (typeProduct != "NVL" && typeProduct != "BTP" && typeProduct != "TP")
            {
                MessageBox.Show("Sai cấu trúc Mã Sản Phẩm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "";

            }
            return typeProduct;
        }

        // =================================================================
        */

        #endregion
    }


}
