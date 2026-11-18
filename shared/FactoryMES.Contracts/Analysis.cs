namespace FactoryMES.Contracts;

public sealed record AnalysisRequest(
    string RequestId,
    string EquipmentId,
    string TargetId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string AnalysisKind
);

public sealed record MockAiTargetCreate(
    string TargetId,
    string DisplayName,
    string ProviderKind = "MOCK"
);

public sealed record AiSettings(
    string ProviderKind,
    string? ApprovedEndpoint,
    string? Model,
    string? CredentialRef,
    long TimeoutMs
);

public sealed record AiConfigContent(
    string TargetId,
    string Kind,
    string ConnectorType,
    long ConfigVersion,
    bool Enabled,
    AiSettings Settings
);

public sealed record AiResolvedConfig(AiConfigContent Content, string ConfigHash);

public sealed record AiTargetView(
    string TargetId,
    string DisplayName,
    bool Enabled,
    long Version,
    AiResolvedConfig Configuration
);

public sealed record AnalysisOptions(DateTime ServerTime, AiTargetView[] Targets);

public sealed record SnapshotDescriptor(
    string SnapshotId,
    string EquipmentId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    long InputVersion,
    string Coverage,
    string InputHash
);

public sealed record AnalysisMetric(
    string MetricCode,
    string? Unit,
    string Minimum,
    string Maximum,
    string Mean,
    string Last,
    [property: System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    )]
    AnalysisMetricObservation? Observation = null
);

// LOCAL에만 포함하는 지표별 표본·분별 관측·마지막 원천시각. 기존 Mock 입력에는 생략한다.
public sealed record AnalysisMetricObservation(
    long SampleCount,
    string Coverage,
    DateTime LastSampledAt
);

public sealed record AnalysisInput(
    int SchemaVersion,
    string EquipmentId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Coverage,
    long SampleCount,
    AnalysisMetric[] Metrics,
    long DowntimeSeconds,
    long AlarmCount
);

public sealed record AnalysisEvidence(
    string Summary,
    string[] Observations,
    string[] Recommendations,
    string InputHash,
    string ConfigHash,
    [property: System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    )]
    MaintenanceAssessment? Maintenance = null
);

public sealed record AiDispatch(
    string JobId,
    string RequestId,
    long Attempt,
    string Operation,
    SnapshotDescriptor Snapshot,
    AiResolvedConfig Configuration,
    DateTime Deadline,
    string CorrelationId
);

public sealed record AnalysisInputEnvelope(
    SnapshotDescriptor Snapshot,
    AnalysisInput Input,
    AiResolvedConfig Configuration,
    string AnalysisKind
);

public sealed record AiResultPayload(
    string JobId,
    long Attempt,
    string Provider,
    string? Model,
    DateTime InputPeriodStart,
    DateTime InputPeriodEnd,
    string DataCoverage,
    long ResultVersion,
    string Risk,
    AnalysisEvidence Evidence,
    bool MockFlag,
    DateTime CompletedAt
);

public sealed record AnalysisResultView(
    string ResultId,
    string JobId,
    long Attempt,
    string Provider,
    string? Model,
    DateTime InputPeriodStart,
    DateTime InputPeriodEnd,
    string DataCoverage,
    long ResultVersion,
    string Risk,
    AnalysisEvidence Evidence,
    bool MockFlag,
    DateTime CompletedAt,
    bool Accepted
);

public sealed record AnalysisJobView(
    string JobId,
    string RequestId,
    string TargetId,
    string Operation,
    string State,
    long Attempt,
    SnapshotDescriptor? Snapshot,
    long ConfigVersion,
    DateTime Deadline,
    long Version,
    string? ErrorCode,
    string? Error,
    string CorrelationId,
    AnalysisResultView[] Results
);





