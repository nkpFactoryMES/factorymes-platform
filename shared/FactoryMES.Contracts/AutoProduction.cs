namespace FactoryMES.Contracts;

// 관리자에게 같은 설비의 활성 COUNTER 매핑만 제공한다. 숫자·ID는 브라우저 정밀도 보존용 문자열이다.
public sealed record AutoCounterOption(
    string MappingId,
    string EquipmentId,
    string TargetId,
    string MetricCode,
    string? Unit,
    long ConfigVersion,
    string? LatestValue,
    DateTime? LatestSampleAt,
    DateTime? LatestReceivedAt,
    long StaleSec
);

// 수집 체크포인트를 읽기 전용으로 표시한다. 조작 API는 CounterRebase 사유 명령뿐이다.
public sealed record AutoCounterView(
    bool AwaitingBaseline,
    string? LatestRawCounter,
    string? StreamId,
    string? LastSequenceNo,
    string? LastTelemetryId,
    DateTime? LastSampleAt,
    DateTime AcceptAfter,
    long ConfigVersion
);

