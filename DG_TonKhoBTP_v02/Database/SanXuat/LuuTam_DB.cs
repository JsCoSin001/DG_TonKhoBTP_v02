#nullable enable

using DG_TonKhoBTP_v02.Core;
using DG_TonKhoBTP_v02.Models;
using DG_TonKhoBTP_v02.Models.SanXuat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace DG_TonKhoBTP_v02.Database.SanXuat
{
    /// <summary>
    /// DB path riêng cho LƯU TẠM. Không được gọi RestoreFromNVL và SaveDraft không
    /// được thay đổi tồn của bất kỳ TTThanhPham nguồn nào.
    /// </summary>
    internal static class LuuTam_DB
    {
        internal sealed class DraftLookupResult
        {
            public long Id { get; set; }
            public string MaBin { get; set; } = string.Empty;
            public int Temp { get; set; }
            public bool Found => Id > 0;
        }

        public static DraftLookupResult FindByMaBin(string maBin)
        {
            var result = new DraftLookupResult();
            string key = (maBin ?? string.Empty).Trim();
            if (key.Length == 0) return result;

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(@"
                SELECT id, MaBin, COALESCE(Temp, 0) AS Temp
                FROM TTThanhPham
                WHERE MaBin = @MaBin COLLATE NOCASE
                LIMIT 1;", conn);
            cmd.Parameters.AddWithValue("@MaBin", key);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return result;

            result.Id = Convert.ToInt64(reader["id"]);
            result.MaBin = Convert.ToString(reader["MaBin"]) ?? string.Empty;
            result.Temp = Convert.ToInt32(reader["Temp"]);
            return result;
        }

        public static DraftLookupResult FindById(long id)
        {
            var result = new DraftLookupResult();

            if (id <= 0)
                return result;

            using var conn = DB_Base.OpenConnection();
            using var cmd = new SQLiteCommand(@"
                SELECT id, MaBin, COALESCE(Temp, 0) AS Temp
                FROM TTThanhPham
                WHERE id = @Id
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return result;

            result.Id = Convert.ToInt64(reader["id"]);
            result.MaBin = Convert.ToString(reader["MaBin"]) ?? string.Empty;
            result.Temp = Convert.ToInt32(reader["Temp"]);

            return result;
        }

        public static bool SaveDraft(DraftSubmitData data, out long draftId, out string error)
        {
            draftId = 0;
            error = string.Empty;
            if (data == null || data.ThongTinThanhPham == null)
            {
                error = "Thiếu dữ liệu lưu tạm.";
                return false;
            }

            SQLiteConnection? conn = null;
            SQLiteTransaction? tx = null;
            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                draftId = data.IsUpdate
                    ? UpdateDraftHeader(conn, tx, data.DraftId, data.ThongTinThanhPham)
                    : InsertDraftHeader(conn, tx, data.ThongTinThanhPham);

                SyncDraftContent(conn, tx, draftId, data);
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                error = DG_TonKhoBTP_v02.Helper.Helper.ShowErrorDatabase(ex, data.ThongTinThanhPham.MaBin);
                draftId = 0;
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        /// <summary>
        /// Lên chính thức từ draft. Kiểm tra tồn và cập nhật tồn trong cùng transaction.
        /// Không backup lịch sử sửa và không RestoreFromNVL vì đây không phải official edit.
        /// </summary>
        public static bool FinalizeDraft(
            long draftId,
            DraftSubmitData data,
            List<LoiNhapLieuData> danhSachLoiNhapLieu,
            out bool inventoryConflict,
            out string error)
        {
            inventoryConflict = false;
            error = string.Empty;
            if (draftId <= 0 || data == null || data.ThongTinThanhPham == null)
            {
                error = "Draft không hợp lệ.";
                return false;
            }

            SQLiteConnection? conn = null;
            SQLiteTransaction? tx = null;
            try
            {
                conn = DB_Base.OpenConnection();
                tx = conn.BeginTransaction();

                VerifyInventorySnapshot(conn, tx, data.NguyenVatLieu, ref inventoryConflict);
                if (inventoryConflict)
                {
                    tx.Rollback();
                    error = "Tồn nguyên vật liệu đã thay đổi. Vui lòng quét/nhập lại toàn bộ nguyên vật liệu.";
                    return false;
                }

                // Đồng bộ lần cuối dữ liệu người dùng đang nhìn thấy trước khi chuyển chính thức.
                UpdateDraftHeader(conn, tx, draftId, data.ThongTinThanhPham);
                SyncDraftContent(conn, tx, draftId, data);

                ApplyInventoryOnce(conn, tx, draftId, data.NguyenVatLieu);
                SaveInputErrors(conn, tx, draftId, danhSachLoiNhapLieu);

                using (var cmd = new SQLiteCommand(@"
                    UPDATE TTThanhPham
                    SET Temp = 0
                    WHERE id = @Id AND Temp = 1;", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Id", draftId);
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Draft đã thay đổi trạng thái hoặc không còn tồn tại.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                error = inventoryConflict
                    ? "Tồn nguyên vật liệu đã thay đổi. Vui lòng quét/nhập lại toàn bộ nguyên vật liệu."
                    : DG_TonKhoBTP_v02.Helper.Helper.ShowErrorDatabase(ex, data.ThongTinThanhPham.MaBin);
                return false;
            }
            finally
            {
                tx?.Dispose();
                conn?.Dispose();
            }
        }

        private static long InsertDraftHeader(SQLiteConnection conn, SQLiteTransaction tx, TTThanhPham m)
        {
            const string sql = @"
                INSERT INTO TTThanhPham
                (DanhSachSP_ID, QC, MaBin, KhoiLuongTruoc, KhoiLuongSau,
                 ChieuDaiTruoc, ChieuDaiSau, CongDoan, GhiChu, HanNoi,
                 DateInsert, Active, Temp)
                VALUES
                (@DanhSachSP_ID, @QC, @MaBin, @KhoiLuongTruoc, @KhoiLuongSau,
                 @ChieuDaiTruoc, @ChieuDaiSau, @CongDoan, @GhiChu, @HanNoi,
                 @DateInsert, @Active, 1);
                SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            AddThanhPhamParameters(cmd, m);
            long id = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
            if (id <= 0) throw new InvalidOperationException("Không tạo được dữ liệu lưu tạm.");
            return id;
        }

        private static long UpdateDraftHeader(SQLiteConnection conn, SQLiteTransaction tx, long draftId, TTThanhPham m)
        {
            const string sql = @"
                UPDATE TTThanhPham SET
                    DanhSachSP_ID=@DanhSachSP_ID, QC=@QC,
                    KhoiLuongTruoc=@KhoiLuongTruoc, KhoiLuongSau=@KhoiLuongSau,
                    ChieuDaiTruoc=@ChieuDaiTruoc, ChieuDaiSau=@ChieuDaiSau,
                    CongDoan=@CongDoan, GhiChu=@GhiChu, HanNoi=@HanNoi,
                    DateInsert=@DateInsert, Active=@Active
                WHERE id=@Id AND Temp=1 AND MaBin=@MaBin COLLATE NOCASE;";
            using var cmd = new SQLiteCommand(sql, conn, tx);
            AddThanhPhamParameters(cmd, m);
            cmd.Parameters.AddWithValue("@Id", draftId);
            if (cmd.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Không cập nhật được draft hoặc MaBin draft đã thay đổi.");
            return draftId;
        }

        private static void AddThanhPhamParameters(SQLiteCommand cmd, TTThanhPham m)
        {
            cmd.Parameters.AddWithValue("@DanhSachSP_ID", m.DanhSachSP_ID);
            cmd.Parameters.AddWithValue("@QC", Db(m.QC));
            cmd.Parameters.AddWithValue("@MaBin", (m.MaBin ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@KhoiLuongTruoc", m.KhoiLuongTruoc);
            cmd.Parameters.AddWithValue("@KhoiLuongSau", m.KhoiLuongSau);
            cmd.Parameters.AddWithValue("@ChieuDaiTruoc", m.ChieuDaiTruoc);
            cmd.Parameters.AddWithValue("@ChieuDaiSau", m.ChieuDaiSau);
            cmd.Parameters.AddWithValue("@CongDoan", m.CongDoan == null ? (object)DBNull.Value : m.CongDoan.Id);
            cmd.Parameters.AddWithValue("@GhiChu", Db(m.GhiChu));
            cmd.Parameters.AddWithValue("@HanNoi", m.HanNoi);
            cmd.Parameters.AddWithValue("@DateInsert", Db(m.DateInsert));
            cmd.Parameters.AddWithValue("@Active", m.Active);
        }

        private static void SyncDraftContent(SQLiteConnection conn, SQLiteTransaction tx, long id, DraftSubmitData data)
        {
            ReplaceTTNVL(conn, tx, id, data.NguyenVatLieu);
            ReplaceCaLamViec(conn, tx, id, data.ThongTinCaLamViec);
            ReplacePheLieu(conn, tx, id, data.ThongTinThanhPham.PheLieu);
            ReplaceCongDoan(conn, tx, id, data.CongDoan);

            LoiDungMay_DB.DongBoDanhSachTheoTTThanhPham(
                conn, tx, id, data.DanhSachLoiDungMay ?? new List<DanhSachLoiDungMay_Model>());

            // 33A: tuyệt đối không ghi DanhSachLoiNhapLieuSX khi lưu tạm.
        }

        private static void ReplaceTTNVL(SQLiteConnection conn, SQLiteTransaction tx, long id, List<TTNVL> items)
        {
            Execute(conn, tx, "DELETE FROM TTNVL WHERE TTThanhPham_ID=@Id;", ("@Id", (object)id));
            if (items == null) return;
            const string sql = @"
                INSERT INTO TTNVL
                (TTThanhPham_ID,BinNVL,QC,DanhSachMaSP_ID,KlBatDau,CdBatDau,KlConLai,CdConLai,
                 DuongKinhSoiDong,SoSoi,KetCauLoi,DuongKinhSoiMach)
                VALUES
                (@Id,@Bin,@QC,@SP,@KLB,@CDB,@KLC,@CDC,@DKD,@SoSoi,@KCL,@DKM);";
            foreach (var m in items.Where(x => x != null))
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Bin", m.BinNVL ?? string.Empty);
                cmd.Parameters.AddWithValue("@QC", Db(m.QC));
                cmd.Parameters.AddWithValue("@SP", Db(m.DanhSachMaSP_ID));
                cmd.Parameters.AddWithValue("@KLB", Db(m.KlBatDau));
                cmd.Parameters.AddWithValue("@CDB", Db(m.CdBatDau));
                cmd.Parameters.AddWithValue("@KLC", Db(m.KlConLai));
                cmd.Parameters.AddWithValue("@CDC", Db(m.CdConLai));
                cmd.Parameters.AddWithValue("@DKD", Db(m.DuongKinhSoiDong));
                cmd.Parameters.AddWithValue("@SoSoi", Db(m.SoSoi));
                cmd.Parameters.AddWithValue("@KCL", Db(m.KetCauLoi));
                cmd.Parameters.AddWithValue("@DKM", Db(m.DuongKinhSoiMach));
                cmd.ExecuteNonQuery();
            }
        }

        private static void ReplaceCaLamViec(SQLiteConnection conn, SQLiteTransaction tx, long id, ThongTinCaLamViec? m)
        {
            Execute(conn, tx, "DELETE FROM ThongTinCaLamViec WHERE TTThanhPham_id=@Id;", ("@Id", (object)id));
            if (m == null || !HasCaData(m)) return;
            using var cmd = new SQLiteCommand(@"
                INSERT INTO ThongTinCaLamViec
                (TTThanhPham_id,NgayBatDau,May,Ca,NguoiLam,ToTruong,QuanDoc,NgayKetThuc,GioBatDau,GioKetThuc,DanhSachMay_ID)
                VALUES (@Id,@NBD,@May,@Ca,@NL,@TT,@QD,@NKT,@GBD,@GKT,@MayId);", conn, tx);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@NBD", Db(m.NgayBatDau?.ToString("yyyy-MM-dd")));
            cmd.Parameters.AddWithValue("@May", Db(m.May));
            cmd.Parameters.AddWithValue("@Ca", Db(m.Ca));
            cmd.Parameters.AddWithValue("@NL", Db(m.NguoiLam));
            cmd.Parameters.AddWithValue("@TT", Db(m.ToTruong));
            cmd.Parameters.AddWithValue("@QD", Db(m.QuanDoc));
            cmd.Parameters.AddWithValue("@NKT", Db(m.NgayKetThuc?.ToString("yyyy-MM-dd")));
            cmd.Parameters.AddWithValue("@GBD", Db(m.GioBatDau?.ToString()));
            cmd.Parameters.AddWithValue("@GKT", Db(m.GioKetThuc?.ToString()));
            cmd.Parameters.AddWithValue("@MayId", m.DanhSachMayId > 0 ? (object)m.DanhSachMayId : DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private static bool HasCaData(ThongTinCaLamViec m) =>
            m.NgayBatDau.HasValue || m.NgayKetThuc.HasValue || m.GioBatDau.HasValue || m.GioKetThuc.HasValue ||
            m.DanhSachMayId > 0 || !string.IsNullOrWhiteSpace(m.May) || !string.IsNullOrWhiteSpace(m.Ca) ||
            !string.IsNullOrWhiteSpace(m.NguoiLam) || !string.IsNullOrWhiteSpace(m.ToTruong) || !string.IsNullOrWhiteSpace(m.QuanDoc);

        private static void ReplacePheLieu(SQLiteConnection conn, SQLiteTransaction tx, long id, PheLieuData? p)
        {
            Execute(conn, tx, "DELETE FROM PheLieu WHERE TTThanhPham_ID=@Id;", ("@Id", (object)id));
            if (p == null || !HasPheData(p)) return;
            using var cmd = new SQLiteCommand(@"
                INSERT INTO PheLieu
                (TTThanhPham_ID,DayPhe_NL,NhuaPhe_NL,DongPhe_NL,GhiChuDayPhe_NL,GhiChuNhuaPhe_NL,GhiChuDongPhe_NL,
                 DayPhe_TP,NhuaPhe_TP,DongPhe_TP,GhiChuDayPhe_TP,GhiChuNhuaPhe_TP,GhiChuDongPhe_TP)
                VALUES (@Id,@DNL,@NNL,@DoNL,@GDNL,@GNNL,@GDoNL,@DTP,@NTP,@DoTP,@GDTP,@GNTP,@GDoTP);", conn, tx);
            cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@DNL", p.DayPhe_NL);
            cmd.Parameters.AddWithValue("@NNL", p.NhuaPhe_NL); cmd.Parameters.AddWithValue("@DoNL", p.DongPhe_NL);
            cmd.Parameters.AddWithValue("@GDNL", Db(p.GhiChuDayPhe_NL)); cmd.Parameters.AddWithValue("@GNNL", Db(p.GhiChuNhuaPhe_NL));
            cmd.Parameters.AddWithValue("@GDoNL", Db(p.GhiChuDongPhe_NL)); cmd.Parameters.AddWithValue("@DTP", p.DayPhe_TP);
            cmd.Parameters.AddWithValue("@NTP", p.NhuaPhe_TP); cmd.Parameters.AddWithValue("@DoTP", p.DongPhe_TP);
            cmd.Parameters.AddWithValue("@GDTP", Db(p.GhiChuDayPhe_TP)); cmd.Parameters.AddWithValue("@GNTP", Db(p.GhiChuNhuaPhe_TP));
            cmd.Parameters.AddWithValue("@GDoTP", Db(p.GhiChuDongPhe_TP)); cmd.ExecuteNonQuery();
        }

        private static bool HasPheData(PheLieuData p) =>
            p.HasData() || !string.IsNullOrWhiteSpace(p.GhiChuDayPhe_NL) ||
            !string.IsNullOrWhiteSpace(p.GhiChuNhuaPhe_NL) || !string.IsNullOrWhiteSpace(p.GhiChuDongPhe_NL) ||
            !string.IsNullOrWhiteSpace(p.GhiChuDayPhe_TP) || !string.IsNullOrWhiteSpace(p.GhiChuNhuaPhe_TP) ||
            !string.IsNullOrWhiteSpace(p.GhiChuDongPhe_TP);

        private static void ReplaceCongDoan(SQLiteConnection conn, SQLiteTransaction tx, long id, SubmitCongDoanData? data)
        {
            // Xóa child của nhóm bọc trước parent CaiDatCDBoc.
            // TTCuonDay_CD tham chiếu CD_BocVo nên xóa tường minh trước để đồng bộ draft
            // không phụ thuộc vào cấu hình PRAGMA foreign_keys của connection.
            Execute(conn, tx, @"DELETE FROM TTCuonDay_CD WHERE CongDoan_ID IN (
                                    SELECT cbv.id
                                    FROM CD_BocVo cbv
                                    INNER JOIN CaiDatCDBoc cdb ON cdb.id = cbv.CaiDatCDBoc_ID
                                    WHERE cdb.TTThanhPham_ID=@Id
                                );
                                DELETE FROM CD_BocLot WHERE CaiDatCDBoc_ID IN (SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID=@Id);
                                DELETE FROM CD_BocMach WHERE CaiDatCDBoc_ID IN (SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID=@Id);
                                DELETE FROM CD_BocVo WHERE CaiDatCDBoc_ID IN (SELECT id FROM CaiDatCDBoc WHERE TTThanhPham_ID=@Id);
                                DELETE FROM CaiDatCDBoc WHERE TTThanhPham_ID=@Id;
                                DELETE FROM CD_KeoRut WHERE TTThanhPham_ID=@Id;
                                DELETE FROM CD_BenRuot WHERE TTThanhPham_ID=@Id;
                                DELETE FROM CD_ChieuXa WHERE TTThanhPham_id=@Id;
                                DELETE FROM CD_GhepLoiQB WHERE TTThanhPham_ID=@Id;", ("@Id", (object)id));

            if (data == null) return;

            object? d = data.ChiTietCongDoan;
            if (d is CD_KeoRut kr)
            {
                InsertKeoRut(conn, tx, id, kr);
                return;
            }
            if (d is CD_BenRuot br)
            {
                InsertBenRuot(conn, tx, id, br);
                return;
            }
            if (d is CD_ChieuXa cx)
            {
                InsertChieuXa(conn, tx, id, cx);
                return;
            }
            if (d is CD_GhepLoiQB gl)
            {
                InsertGhepLoi(conn, tx, id, gl);
                return;
            }

            // Nhóm bọc có thể chỉ nhập phần setup CaiDatCDBoc mà chưa nhập detail.
            // Khi đó vẫn phải lưu setup để mở draft không mất dữ liệu đã nhập.
            bool laChiTietBoc = d is CD_BocLot || d is CD_BocMach || d is CD_BocVo;
            CaiDatCDBoc? setupBoc = data.CaiDatCDBoc;

            if (laChiTietBoc && setupBoc == null)
            {
                // Detail bọc đã có dữ liệu nhưng phần setup còn trống. Migration cho phép
                // các field nghiệp vụ của CaiDatCDBoc = NULL, vì vậy vẫn tạo parent rỗng
                // để không làm mất detail mà người dùng đã nhập.
                setupBoc = new CaiDatCDBoc();
            }

            if ((d == null && setupBoc != null) || laChiTietBoc)
            {
                long bocId = InsertCaiDatBoc(conn, tx, id, setupBoc!);
                if (d is CD_BocLot bl)
                {
                    InsertBocLot(conn, tx, bocId, bl);
                }
                else if (d is CD_BocMach bm)
                {
                    InsertBocMach(conn, tx, bocId, bm);
                }
                else if (d is CD_BocVo bv)
                {
                    long cdBocVoId = InsertBocVo(conn, tx, bocId, bv);
                    InsertTTCuonDayCD(conn, tx, cdBocVoId, bv.TTCuonDay_CD);
                }
            }
        }

        private static void InsertKeoRut(SQLiteConnection c, SQLiteTransaction t, long id, CD_KeoRut m) =>
            ExecInsert(c,t,"INSERT INTO CD_KeoRut(TTThanhPham_ID,DKTrucX,DKTrucY,NgoaiQuan,TocDo,DienApU,DongDienU) VALUES(@Id,@A,@B,@C,@D,@E,@F);",
                ("@Id",id),("@A",m.DKTrucX),("@B",m.DKTrucY),("@C",m.NgoaiQuan),("@D",m.TocDo),("@E",m.DienApU),("@F",m.DongDienU));
        private static void InsertBenRuot(SQLiteConnection c, SQLiteTransaction t, long id, CD_BenRuot m) =>
            ExecInsert(c,t,"INSERT INTO CD_BenRuot(TTThanhPham_ID,DKSoi,SoSoi,ChieuXoan,BuocBen) VALUES(@Id,@A,@B,@C,@D);",
                ("@Id",id),("@A",m.DKSoi),("@B",m.SoSoi),("@C",m.ChieuXoan),("@D",m.BuocBen));
        private static void InsertChieuXa(SQLiteConnection c, SQLiteTransaction t, long id, CD_ChieuXa m) =>
            ExecInsert(c,t,"INSERT INTO CD_ChieuXa(TTThanhPham_id,LucCangThu,LucCangTha,SoVong,TocDo,NLCX,DongDien,LieuChieu,NgoaiQuan,DoChiuNhiet) VALUES(@Id,@A,@B,@C,@D,@E,@F,@G,@H,@I);",
                ("@Id",id),("@A",m.LucCangThu),("@B",m.LucCangTha),("@C",m.SoVong),("@D",m.TocDo),("@E",m.NLCX),("@F",m.DongDien),("@G",m.LieuChieu),("@H",m.NgoaiQuan),("@I",m.DoChiuNhiet));
        private static void InsertGhepLoi(SQLiteConnection c, SQLiteTransaction t, long id, CD_GhepLoiQB m) =>
            ExecInsert(c,t,"INSERT INTO CD_GhepLoiQB(TTThanhPham_ID,ChieuXoan,GoiCachMep,DKBTP,DoRongBang,DoDayBang) VALUES(@Id,@A,@B,@C,@D,@E);",
                ("@Id",id),("@A",m.ChieuXoan),("@B",m.GoiCachMep),("@C",m.DKBTP),("@D",m.DoRongBang),("@E",m.DoDayBang));

        private static long InsertCaiDatBoc(SQLiteConnection c, SQLiteTransaction t, long id, CaiDatCDBoc m)
        {
            const string sql = @"INSERT INTO CaiDatCDBoc(TTThanhPham_ID,MangNuoc,PuliDanDay,BoDemMet,MayIn,v1,v2,v3,v4,v5,v6,Co,Dau1,Dau2,Khuon,BinhSay,DKKhuon1,DKKhuon2,TTNhua,KTDKLan1,KTDKLan2,KTDKLan3,DiemMongLan1,DiemMongLan2)
                VALUES(@Id,@A,@B,@C,@D,@E,@F,@G,@H,@I,@J,@K,@L,@M,@N,@O,@P,@Q,@R,@S,@T,@U,@V,@W); SELECT last_insert_rowid();";
            using var cmd = new SQLiteCommand(sql,c,t);
            object?[] vals={id,m.MangNuoc,m.PuliDanDay,m.BoDemMet,m.MayIn,m.v1,m.v2,m.v3,m.v4,m.v5,m.v6,m.Co,m.Dau1,m.Dau2,m.Khuon,m.BinhSay,m.DKKhuon1,m.DKKhuon2,m.TTNhua,m.KTDKLan1,m.KTDKLan2,m.KTDKLan3,m.DiemMongLan1,m.DiemMongLan2};
            string[] names={"@Id","@A","@B","@C","@D","@E","@F","@G","@H","@I","@J","@K","@L","@M","@N","@O","@P","@Q","@R","@S","@T","@U","@V","@W"};
            for(int i=0;i<names.Length;i++) cmd.Parameters.AddWithValue(names[i],Db(vals[i]));
            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        }
        private static void InsertBocLot(SQLiteConnection c, SQLiteTransaction t, long id, CD_BocLot m) => ExecInsert(c,t,"INSERT INTO CD_BocLot(CaiDatCDBoc_ID,DoDayTBLot) VALUES(@Id,@A);",("@Id",id),("@A",m.DoDayTBLot));
        private static void InsertBocMach(SQLiteConnection c, SQLiteTransaction t, long id, CD_BocMach m) => ExecInsert(c,t,"INSERT INTO CD_BocMach(CaiDatCDBoc_ID,NgoaiQuan,LanDanhThung,SoMet,Mau) VALUES(@Id,@A,@B,@C,@D);",("@Id",id),("@A",m.NgoaiQuan),("@B",m.LanDanhThung),("@C",m.SoMet),("@D",m.Mau));
        private static long InsertBocVo(SQLiteConnection c, SQLiteTransaction t, long id, CD_BocVo m)
        {
            using var cmd = new SQLiteCommand(
                "INSERT INTO CD_BocVo(CaiDatCDBoc_ID,DayVoTB,InAn) VALUES(@Id,@A,@B); SELECT last_insert_rowid();",
                c, t);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@A", Db(m.DayVoTB));
            cmd.Parameters.AddWithValue("@B", Db(m.InAn));
            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        }

        private static void InsertTTCuonDayCD(SQLiteConnection conn, SQLiteTransaction tx, long cdBocVoId, List<ThongTinCuonDay>? items)
        {
            if (cdBocVoId <= 0 || items == null || items.Count == 0) return;

            const string sql = @"
                INSERT INTO TTCuonDay_CD
                    (SoCuon,TongChieuDai,SoDau,SoCuoi,GhiChu,CongDoan_ID,TTLo_ID)
                VALUES
                    (@SoCuon,@TongChieuDai,@SoDau,@SoCuoi,@GhiChu,@CongDoan_ID,@TTLo_ID);";

            foreach (ThongTinCuonDay item in items.Where(x => x != null))
            {
                using var cmd = new SQLiteCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@SoCuon", item.SoCuon);
                cmd.Parameters.AddWithValue("@TongChieuDai", item.TongChieuDai);
                cmd.Parameters.AddWithValue("@SoDau", Db(item.SoDau));
                cmd.Parameters.AddWithValue("@SoCuoi", Db(item.soCuoi));
                cmd.Parameters.AddWithValue("@GhiChu", Db(item.Ghichu));
                cmd.Parameters.AddWithValue("@CongDoan_ID", cdBocVoId);
                cmd.Parameters.AddWithValue("@TTLo_ID", Db(item.TTLo_ID));
                cmd.ExecuteNonQuery();
            }
        }

        private static void VerifyInventorySnapshot(SQLiteConnection conn, SQLiteTransaction tx, List<TTNVL> items, ref bool conflict)
        {
            foreach (var nvl in items ?? new List<TTNVL>())
            {
                using var cmd = new SQLiteCommand(@"SELECT KhoiLuongSau,ChieuDaiSau FROM TTThanhPham WHERE MaBin=@Bin COLLATE NOCASE AND Temp=0 LIMIT 1;", conn, tx);
                cmd.Parameters.AddWithValue("@Bin", (nvl.BinNVL ?? string.Empty).Trim());
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { conflict = true; return; }
                if (string.Equals(nvl.DonVi, "KG", StringComparison.OrdinalIgnoreCase))
                {
                    double? current = r["KhoiLuongSau"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["KhoiLuongSau"]);
                    if (!Same(current, nvl.KlBatDau)) { conflict = true; return; }
                }
                else if (string.Equals(nvl.DonVi, "M", StringComparison.OrdinalIgnoreCase))
                {
                    double? current = r["ChieuDaiSau"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["ChieuDaiSau"]);
                    if (!Same(current, nvl.CdBatDau)) { conflict = true; return; }
                }
                else { conflict = true; return; }
            }
        }

        private static bool Same(double? a, double? b) => a.HasValue == b.HasValue && (!a.HasValue || Math.Abs(a.Value-b!.Value) < 0.0000001d);

        private static void ApplyInventoryOnce(SQLiteConnection conn, SQLiteTransaction tx, long finalId, List<TTNVL> items)
        {
            foreach (var nvl in items ?? new List<TTNVL>())
            {
                using var cmd = new SQLiteCommand(@"
                    UPDATE TTThanhPham SET
                        KhoiLuongSau=COALESCE(@KL,KhoiLuongSau),
                        ChieuDaiSau=COALESCE(@CD,ChieuDaiSau),
                        QC=@QC, LastEdit_id=@LastEdit
                    WHERE MaBin=@Bin COLLATE NOCASE AND Temp=0;", conn, tx);
                cmd.Parameters.AddWithValue("@KL", Db(nvl.KlConLai));
                cmd.Parameters.AddWithValue("@CD", Db(nvl.CdConLai));
                cmd.Parameters.AddWithValue("@QC", Db(nvl.QC));
                cmd.Parameters.AddWithValue("@LastEdit", finalId);
                cmd.Parameters.AddWithValue("@Bin", (nvl.BinNVL ?? string.Empty).Trim());
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không cập nhật được tồn NVL: " + nvl.BinNVL);
            }
        }

        private static void SaveInputErrors(SQLiteConnection conn, SQLiteTransaction tx, long id, List<LoiNhapLieuData> items)
        {
            Execute(conn,tx,"DELETE FROM DanhSachLoiNhapLieuSX WHERE TTThanhpham_id=@Id;",("@Id",(object)id));
            foreach(var x in (items ?? new List<LoiNhapLieuData>()).Where(x=>x!=null && !string.IsNullOrWhiteSpace(x.NoiDungLoi)).GroupBy(x=>x.NoiDungLoi.Trim()).Select(g=>g.First()))
            {
                using var cmd=new SQLiteCommand("INSERT INTO DanhSachLoiNhapLieuSX(TTThanhpham_id,NoiDungLoi,LyDoLoi) VALUES(@Id,@ND,@LD);",conn,tx);
                cmd.Parameters.AddWithValue("@Id",id); cmd.Parameters.AddWithValue("@ND",x.NoiDungLoi.Trim()); cmd.Parameters.AddWithValue("@LD",x.LyDoLoi?.Trim() ?? string.Empty); cmd.ExecuteNonQuery();
            }
        }

        private static object Db(object? value)
        {
            if (value == null) return DBNull.Value;
            if (value is string text)
                return string.IsNullOrWhiteSpace(text) ? DBNull.Value : (object)text.Trim();
            return value;
        }
        private static void ExecInsert(SQLiteConnection c, SQLiteTransaction t, string sql, params (string name, object? value)[] ps)
        { using var cmd=new SQLiteCommand(sql,c,t); foreach(var p in ps) cmd.Parameters.AddWithValue(p.name,Db(p.value)); cmd.ExecuteNonQuery(); }
        private static void Execute(SQLiteConnection c, SQLiteTransaction t, string sql, params (string name, object value)[] ps)
        { using var cmd=new SQLiteCommand(sql,c,t); foreach(var p in ps) cmd.Parameters.AddWithValue(p.name,p.value); cmd.ExecuteNonQuery(); }
    }
}

#nullable restore
