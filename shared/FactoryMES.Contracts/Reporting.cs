namespace FactoryMES.Contracts;

public sealed record DailyProductionView(
    string BusinessDate,
    string EquipmentId,
    string ProductId,
    long TotalQty,
    long RejectQty,
    DateTime RefreshedAt
);

public sealed record JobRunView(
    string JobRunId,
    string JobKey,
    DateTime ScheduledFor,
    long Attempt,
    string Status,
    DateTime StartedAt,
    DateTime? EndedAt,
    long? AffectedRows,
    string? Error
);

public sealed record ReportingRebuildRequest(string BusinessDate);

public sealed record ReportingRunRequest(
    string BusinessDate,
    string ClientRequestId,
    string Reason
);

public sealed record ReportingStatusView(string BusinessDate, JobRunView? LatestRun);

public sealed record ReportingDispatchResult(
    int Examined,
    int Completed,
    int Failed
);

