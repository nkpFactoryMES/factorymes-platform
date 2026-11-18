namespace FactoryMES.Contracts;

public sealed record OutboxRetryRequest(
    string ClientRequestId,
    string ExpectedStateHash,
    string Reason
);

public sealed record OutboxRetryState(
    OutboxView Event,
    string StateHash,
    bool CanRetry,
    string? BlockReason
);

