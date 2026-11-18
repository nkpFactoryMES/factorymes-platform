namespace FactoryMES.Contracts;

public sealed record DeliveryView(
    string DeliveryId,
    string NotificationId,
    string RecipientId,
    string Channel,
    string DestinationKey,
    string Status,
    long Attempts,
    DateTime? NextAttemptAt,
    DateTime? SentAt,
    string? LastError,
    long Version
);

public sealed record DispatchBatchResult(
    int Examined,
    int Delivered,
    int RetryPending,
    int Failed
);

public sealed record MockNotificationRequest(
    string ClientRequestId,
    string Scenario
);

public sealed record MockNotificationResult(
    string NotificationId,
    string ClientRequestId
);

public sealed record DeliveryMockStatus(
    string DeliveryId,
    string Outcome,
    string? ReceiptId,
    DateTime? AcceptedAt
);

public sealed record MockReceiptView(
    string DeliveryId,
    string ReceiptId,
    string Provider,
    string Channel,
    string DestinationKey,
    string Subject,
    string Body,
    string MessageHash,
    DateTime AcceptedAt
);

public sealed record OutboxView(
    string OutboxId,
    string EventId,
    string Type,
    string AggregateId,
    string Status,
    long Attempts,
    DateTime? NextAttemptAt,
    DateTime? DeliveredAt,
    string? LastError
);

