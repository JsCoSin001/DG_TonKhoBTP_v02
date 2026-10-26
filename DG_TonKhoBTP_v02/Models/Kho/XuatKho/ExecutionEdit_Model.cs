using System;
using System.Collections.Generic;

namespace DG_TonKhoBTP_v02.Models.Kho.XuatKho
{
    internal sealed class ExecutionDetailQuery
    {
        public long PlanId { get; set; }
        public long ItemId { get; set; }
        public long? SpecificationId { get; set; }
        public long? PartialReelId { get; set; }
        public ExecutionDetailQuery(long planId, long itemId, long? specificationId, long? partialReelId)
        { PlanId = planId; ItemId = itemId; SpecificationId = specificationId; PartialReelId = partialReelId; }
    }
    internal sealed class ExecutionDetailSnapshot
    {
        public ExecutionDetailQuery Query { get; set; } = null!;
        public string PlanStatus { get; set; } = "";
        public bool EditingEnabled { get; set; }
        public List<ExecutionDetailRow> Rows { get; set; } = new List<ExecutionDetailRow>();
        public Dictionary<long, ExecutionReelState> Reels { get; set; } = new Dictionary<long, ExecutionReelState>();
        public Dictionary<string, long> Versions { get; set; } = new Dictionary<string, long>();
        public List<ExecutionPerformer> Users { get; set; } = new List<ExecutionPerformer>();
    }
    internal sealed class ExecutionPerformer
    {
        public long UserId { get; set; }
        public string Username { get; set; } = "";
        public bool IsActive { get; set; }
        public string DisplayName => Username + (IsActive ? "" : " (ngừng hoạt động)");
    }
    internal sealed class ExecutionDetailRow
    {
        public string RowKey { get; set; } = "";
        public string Kind { get; set; } = "";
        public string OperationToken { get; set; } = "";
        public long HistoryId { get; set; }
        public long? DetailId { get; set; }
        public long? GroupId { get; set; }
        public long? ReelId { get; set; }
        public int Quantity { get; set; }
        public int Length { get; set; }
        public bool Done { get; set; }
        public string PerformerUsername { get; set; } = "";
        public string PerformedAt { get; set; } = "";
        public string RawQr { get; set; } = "";
        public string SourceKind { get; set; } = "";
        public long SourceId { get; set; }
        public int BeforeStart { get; set; }
        public int BeforeEnd { get; set; }
        public int CutStart { get; set; }
        public int CutEnd { get; set; }
        public int AfterStart { get; set; }
        public int AfterEnd { get; set; }
        public string Status => Done ? "Đã thực hiện" : "Chưa thực hiện";
        public string Description => Kind == "TAKE" ? "Lấy nguyên cuộn" :
            SourceKind == "CUON_CHAN" ? "Cắt từ cuộn chẵn" : "Cắt cuộn lẻ";
    }
    internal sealed class ExecutionReelState
    {
        public long ReelId { get; set; }
        public string Code { get; set; } = "";
        public long SourceId { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string Status { get; set; } = "";
        public long Length => Math.Abs((long)End - Start);
        public int Direction => Start <= End ? 1 : -1;
    }
    internal sealed class ExecutionRowEdit
    {
        public string OperationToken { get; set; } = "";
        public string NewPerformerUsername { get; set; } = "";
        public bool Undo { get; set; }
        public string Reason { get; set; } = "";
        public string Note { get; set; } = "";
    }
    internal sealed class ReelMarkerEdit
    {
        public long ReelId { get; set; }
        public int NewEnd { get; set; }
        public int NewDirection { get; set; }
        public string Reason { get; set; } = "";
        public string Note { get; set; } = "";
    }
    internal sealed class ExecutionEditRequest
    {
        public Guid ChangeId { get; set; } = Guid.NewGuid();
        public ExecutionDetailQuery Query { get; set; } = null!;
        public Dictionary<string, long> ExpectedVersions { get; set; } = new Dictionary<string, long>();
        public List<ExecutionRowEdit> RowEdits { get; set; } = new List<ExecutionRowEdit>();
        public List<ReelMarkerEdit> ReelEdits { get; set; } = new List<ReelMarkerEdit>();
    }
    internal sealed class ExecutionEditResult
    {
        public bool Success { get; set; }
        public bool Conflict { get; set; }
        public Dictionary<string, string> ErrorsByKey { get; set; } = new Dictionary<string, string>();
        public ExecutionDetailSnapshot? NewSnapshot { get; set; }
        public List<long> ChangedPlanIds { get; set; } = new List<long>();
        public List<long> ChangedReelIds { get; set; } = new List<long>();
        public string Message => string.Join(Environment.NewLine, ErrorsByKey.Values);
    }
}
