using System.Text.Json.Serialization;

namespace FactoryMES.Contracts;

public sealed record MappingInput(
    string? MappingId,
    string MetricCode,
    string SourcePath,
    string DataType,
    string ValueKind,
    string? Unit,
    string Scale,
    string Offset,
    bool Enabled
);

public sealed record EquipmentSettings(
    string Protocol,
    string ApprovedEndpoint,
    long PollIntervalMs,
    long TimeoutMs,
    long StaleSec,
    string? CredentialRef,
    MappingInput[] Mappings
);

public sealed record EquipmentTargetCreate(
    string TargetId,
    string DisplayName,
    string ConnectorType,
    bool Enabled,
    string Kind,
    string EquipmentId,
    EquipmentSettings Settings
);

public sealed record TargetConfigUpdate(
    long ExpectedVersion,
    long ExpectedConfigVersion,
    string DisplayName,
    bool Enabled,
    string Reason,
    EquipmentSettings Settings
);

public sealed record TargetStateUpdate(
    long ExpectedVersion,
    long ExpectedConfigVersion,
    bool Enabled,
    string Reason
);

public sealed record EquipmentConfigContent(
    string TargetId,
    string Kind,
    string EquipmentId,
    string ConnectorType,
    long ConfigVersion,
    bool Enabled,
    EquipmentSettings Settings
);

public sealed record EquipmentResolvedConfig(
    EquipmentConfigContent Content,
    string ConfigHash
);

public sealed record TargetView(
    string TargetId,
    string Kind,
    string DisplayName,
    string? EquipmentId,
    bool Enabled,
    bool Deleted,
    long ConfigVersion,
    long Version,
    long AppliedConfigVersion,
    string? ConnectionState,
    DateTime? LastHeartbeatAt,
    EquipmentResolvedConfig Config
);

public sealed record LeaseRequest(
    string TargetId,
    string InstanceId,
    long ConfigVersion,
    string ConfigHash
);

public sealed record LeaseResponse(
    string TargetId,
    string InstanceId,
    long ConfigVersion,
    string ConfigHash,
    string CollectionAuthorization,
    DateTime ValidFrom,
    DateTime ExpiresAt,
    DateTime ReceiveUntil
);

public sealed record ConfigChangeData(
    string TargetId,
    long ConfigVersion,
    EquipmentResolvedConfig Configuration
);

public sealed record ConfigChanged(
    string EventId,
    string Type,
    long SchemaVersion,
    string Source,
    DateTime OccurredAt,
    string CorrelationId,
    ConfigChangeData Data
);

public sealed record ConfigEventAck(
    string EventId,
    string TargetId,
    long ConfigVersion,
    string Outcome
);

public sealed record ConfigApply(
    EquipmentResolvedConfig Configuration,
    string InstanceId
);

public sealed record ConfigAck(
    string TargetId,
    long AppliedConfigVersion,
    bool ConfigurationAccepted,
    string ConnectionState
);

public sealed record TelemetryData(
    string EquipmentId,
    string TargetId,
    string InstanceId,
    string StreamId,
    string SequenceNo,
    long ConfigVersion,
    string ConfigHash,
    CollectedMetric[] Metrics
);

public sealed record CollectedEvent(
    string EventId,
    string Type,
    long SchemaVersion,
    string Source,
    DateTime OccurredAt,
    string CorrelationId,
    TelemetryData Data
);

public sealed record TelemetryBatch(
    string CollectionAuthorization,
    EquipmentResolvedConfig Configuration,
    CollectedEvent[] Events
);

public sealed record BatchAckItem(
    string EventId,
    string TelemetryId,
    string Outcome,
    DateTime ReceivedAt
);

public sealed record TelemetryBatchAck(
    BatchAckItem[] Items,
    string CorrelationId
);

public sealed record RuntimeReport(
    string TargetId,
    string InstanceId,
    long AppliedConfigVersion,
    string ConnectionState,
    string? OperatingState,
    DateTime ReportedAt,
    string? LastErrorCode,
    string? LastError
);

public sealed record SampleView(
    string TelemetryId,
    string EventId,
    string StreamId,
    string SequenceNo,
    long ConfigVersion,
    DateTime OccurredAt,
    DateTime ReceivedAt,
    CollectedMetric[] Metrics
);

public sealed record MockSample(
    string EquipmentCode,
    string TotalCount,
    string Temperature,
    string OperatingState,
    bool MockFlag,
    DateTime SampledAt
);

public sealed record CollectedMetric(
    string MetricCode,
    string MappingId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NumericValue,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TextValue,
    string? Unit,
    string Quality
);

