using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using DG_TonKhoBTP_v02.Models.Kho.XuatKho;
using System.Linq;
using System.Web.Script.Serialization;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
    internal sealed class ExecutionEntityTransition
    {
        public string Table { get; set; } = "";
        public string KeyColumn { get; set; } = "";
        public object Key { get; set; } = null!;
        public Dictionary<string, object>? Before { get; set; }
        public Dictionary<string, object>? After { get; set; }
    }

    internal static partial class ExecutionEdit_DB
    {
        private static Dictionary<string, object>? ReadEntity(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEntityTransition entity)
        {
            // Table and key identifiers only come from the fixed calls below. Values are parameters.
            var rows = Table(conn, tx, "SELECT * FROM " + entity.Table + " WHERE " + entity.KeyColumn + "=@id", "@id", entity.Key).Rows;
            return rows.Count == 0 ? null : ExecutionEditAudit.Row(rows[0]);
        }
        private static List<ExecutionEntityTransition> CaptureAffectedEntities(SQLiteConnection conn, SQLiteTransaction tx,
            ExecutionEditRequest request, ExecutionDetailSnapshot snapshot, ExecutionUndoPlan plan)
        {
            var entities = new List<ExecutionEntityTransition>();
            void Add(string table, string keyColumn, object key)
            {
                if (entities.Any(x => x.Table == table && Equals(x.Key, key))) return;
                var entity = new ExecutionEntityTransition { Table = table, KeyColumn = keyColumn, Key = key };
                entity.Before = ReadEntity(conn, tx, entity); entities.Add(entity);
            }
            var editedRows = snapshot.Rows.Where(x => request.RowEdits.Any(e => e.OperationToken == x.OperationToken)).ToList();
            foreach (var row in editedRows)
            {
                Add(row.Kind == "CUT" ? "LichSuCat" : "LichSuLayCuon", "id", row.HistoryId);
                Add("B3ThaoTac", "MaThaoTac", row.OperationToken);
                if (row.GroupId.HasValue) Add("KeHoachCatNhom", "id", row.GroupId.Value);
            }
            foreach (long reel in editedRows.Where(x => x.ReelId.HasValue).Select(x => x.ReelId!.Value)
                .Concat(request.ReelEdits.Select(x => x.ReelId)).Concat(plan.ReelRestores.Select(x => x.ReelId)).Distinct())
                Add("TonCuonLe", "id", reel);
            foreach (long source in editedRows.Select(x => x.SourceId).Concat(request.ReelEdits.Select(x => snapshot.Reels[x.ReelId].SourceId)).Where(x => x > 0).Distinct())
                Add("TTCuonDay", "id", source);
            foreach (string qr in editedRows.Select(x => x.RawQr).Concat(plan.UndoOperations.Select(x => x.Qr)).Where(x => x.Length > 0).Distinct())
                Add("B3QrSuDung", "RawQr", qr);
            return entities;
        }
        private static void CompleteBatchAudit(SQLiteConnection conn, SQLiteTransaction tx, ExecutionEditRequest request,
            Dictionary<string, long> beforeVersions, List<ExecutionEntityTransition> effects)
        {
            foreach (var effect in effects) effect.After = ReadEntity(conn, tx, effect);
            var afterVersions = Table(conn, tx, "SELECT Khoa,GiaTri FROM B3PhienBanDuLieu").Rows.Cast<DataRow>()
                .ToDictionary(r => T(r, "Khoa"), r => N(r, "GiaTri"));
            var events = Table(conn, tx, "SELECT * FROM B2DieuChinhSuKien WHERE MaLanLuu=@batch ORDER BY id", "@batch", request.ChangeId.ToString("D"))
                .Rows.Cast<DataRow>().Select(ExecutionEditAudit.Row).ToList();
            Exec(conn, tx, "INSERT INTO B2DieuChinhKetQua(MaLanLuu,PhienBan,NoiDungJson) VALUES(@id,1,@body)",
                "@id", request.ChangeId.ToString("D"), "@body", ExecutionEditAudit.Json(new {
                    version = 1, expected_versions = request.ExpectedVersions, before_versions = beforeVersions,
                    after_versions = afterVersions, entity_effects = effects, events }));
        }
    }

    internal static class ExecutionEditAudit
    {
        internal static string Json(object value) => new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Serialize(value);
        internal static Dictionary<string, object> Parse(string value) =>
            new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Deserialize<Dictionary<string, object>>(value);
        internal static Dictionary<string, object> Row(DataRow row) => row.Table.Columns.Cast<DataColumn>()
            .ToDictionary(x => x.ColumnName, x => row[x] == DBNull.Value ? null! : row[x]);
        internal static Dictionary<string, object>? Object(Dictionary<string, object> value, string key) =>
            value.TryGetValue(key, out object nested) ? nested as Dictionary<string, object> : null;
        internal static long Number(Dictionary<string, object> value, string key) =>
            value.TryGetValue(key, out object result) && result != null ? Convert.ToInt64(result) : 0;
    }
}
