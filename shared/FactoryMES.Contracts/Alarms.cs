namespace FactoryMES.Contracts;

public sealed record RuleWrite(
    string EquipmentId,
    string TargetId,
    string? MappingId,
    string Kind,
    string Severity,
    string? TriggerHigh,
    string? ClearBelow,
    long? HoldSec,
    long? ClearHoldSec,
    bool Enabled
);

public sealed record RuleUpdate(
    long ExpectedVersion,
    string EquipmentId,
    string TargetId,
    string? MappingId,
    string Kind,
    string Severity,
    string? TriggerHigh,
    string? ClearBelow,
    long? HoldSec,
    long? ClearHoldSec,
    bool Enabled
);

public sealed record RuleView(
    string RuleId,
    string EquipmentId,
    string TargetId,
    string? MappingId,
    string Kind,
    string Severity,
    string? TriggerHigh,
    string? ClearBelow,
    long? HoldSec,
    long? ClearHoldSec,
    bool Enabled,
    long Version,
    DateTime? SuppressUntil
);

public sealed record AlarmView(
    string AlarmId,
    string EquipmentId,
    string RuleId,
    string Status,
    string Kind,
    string Severity,
    DateTime OpenedAt,
    DateTime? RecoveredAt,
    string? AckBy,
    DateTime? AckAt,
    DateTime? ClosedAt,
    string? CloseKind,
    string? CloseReason,
    long Version
);

public sealed record NotificationView(
    string NotificationId,
    string EventId,
    string Title,
    string Body,
    string? AlarmId,
    string? OrderId,
    string? AnalysisResultId,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public sealed record AckResponse(
    bool Accepted,
    string CorrelationId
);

public sealed record DispatchBatchRequest(
    int Limit
);

public sealed record ConnectionWatchResult(
    int Examined,
    int Opened,
    int Recovered,
    int Unchanged,
    DateTime CheckedAt
);


