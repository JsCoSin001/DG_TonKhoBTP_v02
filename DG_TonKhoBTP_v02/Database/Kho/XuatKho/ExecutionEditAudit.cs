using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Script.Serialization;

namespace DG_TonKhoBTP_v02.Database.Kho.XuatKho
{
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
