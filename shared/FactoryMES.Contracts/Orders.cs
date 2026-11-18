namespace FactoryMES.Contracts;
public sealed record PageResult<T>(T[] Items, string? NextCursor);
public sealed record SessionUser(
    string UserId,
    string Login,
    string Name,
    string[] Roles
);
public sealed record ProductChoice(
    string ProductId,
    string Code,
    string Name,
    string Unit,
    bool Active,
    long Version
);
public sealed record EquipmentChoice(
    string EquipmentId,
    string Code,
    string Name,
    string LineId,
    bool Active,
    long Version
);
public sealed record UserChoice(
    string UserId,
    string Login,
    string Name,
    string[] Roles,
    string? Email,
    bool Active,
    long Version
);
public sealed record ChecklistDefinition(
    int Seq,
    string Label,
    bool Required
);
public sealed record OrderCreate(
    string ProductId,
    string EquipmentId,
    string AssigneeId,
    string? OperationLabel,
    long TargetQty,
    string CountMode,
    string? CounterMappingId,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    ChecklistDefinition[] Checklist
);
public sealed record Command(long ExpectedVersion);
public sealed record ReasonCommand(long ExpectedVersion, string Reason);
public sealed record SubmitRequest(long ExpectedVersion, string? CompletionReason);
public sealed record CheckInput(
    string CheckId,
    bool Checked,
    string? Comment
);
public sealed record ChecklistUpdate(long ExpectedVersion, CheckInput[] Items);
public sealed record ChecklistItem(
    string CheckId,
    long Seq,
    string Label,
    bool Required,
    bool Checked,
    DateTime? CheckedAt,
    string? Comment
);
public sealed record EntryInput(
    string ClientEntryId,
    string Kind,
    long Qty,
    string? ReversalOf,
    string? CorrectionId,
    string? Reason,
    DateTime OccurredAt
);
public sealed record EntryBatch(long ExpectedVersion, EntryInput[] Entries);
public sealed record EntryView(
    string EntryId,
    string OrderId,
    string ClientEntryId,
    string Kind,
    long Qty,
    string? ReversalOf,
    string? CorrectionId,
    string? Reason,
    DateTime OccurredAt,
    string RecordedBy,
    string Source = "MANUAL",
    string? TelemetryId = null
);
public sealed record EntryBatchResult(
    string OrderId,
    long Version,
    bool Duplicate,
    EntryView[] Entries,
    long? TotalQty,
    long RejectQty
);
public sealed record OrderView(
    string OrderId,
    string OrderNo,
    string ProductId,
    string EquipmentId,
    string AssigneeId,
    string? OperationLabel,
    long TargetQty,
    string CountMode,
    string? CounterMappingId,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    string Status,
    long Version,
    ChecklistItem[] Checklist,
    object? StartCounter,
    object? EndCounter,
    bool CounterInvalid,
    long? TotalQty,
    long RejectQty,
    long? GoodQty,
    string? CompletionReason,
    AutoCounterView? AutoCounter = null
);
