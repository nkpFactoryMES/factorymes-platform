namespace FactoryMES.Contracts;

public sealed record PauseRequest(
    long ExpectedVersion,
    string ReasonCode,
    string? Note,
    string? AlarmId
);

public sealed record CorrectionOpen(long ExpectedVersion, string Reason);

public sealed record CorrectionResolve(
    long ExpectedVersion,
    long ExpectedOrderVersion,
    string ResolutionNote
);

public sealed record CorrectionView(
    string CorrectionId,
    string OrderId,
    string Status,
    string Reason,
    long Version,
    long OrderVersion,
    string RequestedBy,
    string? ResolvedBy,
    DateTime? ResolvedAt
);

