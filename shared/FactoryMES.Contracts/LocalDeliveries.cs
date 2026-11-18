namespace FactoryMES.Contracts;

public sealed record LocalNotificationRequest(string ClientRequestId, string Scenario);

public sealed record LocalDeliveryStatus(
    string DeliveryId,
    string Outcome,
    string? ReceiptId,
    DateTime? AcceptedAt
);

public sealed record LocalReceiptView(
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

public sealed record LocalDeliveryOutcome(
    string Outcome,
    string? ErrorCode,
    LocalReceiptView? Receipt,
    int? RetryAfterSeconds = null
);

// 외부 주소·비밀을 받지 않고 고정 로컬 수신 서버만 사용한다.
public static class LocalDeliveryPolicy
{
    public const string SmtpHost = "mailpit";
    public const int SmtpPort = 1025;
    public const string ChatEndpoint = "http://chat-receiver:8080/hooks/factorymes";
    public const int TimeoutSeconds = 2;

    public static bool ValidScenario(string? scenario) => scenario is
        "SUCCESS" or "CHAT_TRANSIENT_ONCE" or "CHAT_ALWAYS_FAIL" or "CHAT_PERMANENT_FAIL";

    public static string Destination(string channel, string recipient) =>
        "local." + channel.ToLowerInvariant() + ".user." + recipient;

    public static string Target(
        string channel,
        string recipient,
        string scenario
    ) =>
        "local://capture/" + channel.ToLowerInvariant() + "/" + recipient + "/" + scenario;

    public static bool IsLocal(string target) => target.StartsWith("local://", StringComparison.Ordinal);

    public static bool ValidTarget(
        string channel,
        string recipient,
        string destination,
        string target
    )
    {
        var scenario = target.Split('/').LastOrDefault();
        return channel is "EMAIL" or "CHAT"
            && long.TryParse(recipient, out var id) && id > 0
            && id.ToString(System.Globalization.CultureInfo.InvariantCulture) == recipient
            && ValidScenario(scenario)
            && destination == Destination(channel, recipient)
            && target == Target(
                channel,
                recipient,
                scenario!
            );
    }
}

