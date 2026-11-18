namespace FactoryMES.Contracts;

public sealed record ActionCreate(
    long ExpectedVersion,
    string OwnerId,
    string Content
);

public sealed record ActionStart(
    long ExpectedVersion,
    long ExpectedAlarmVersion
);

public sealed record ActionComplete(
    long ExpectedVersion,
    long ExpectedAlarmVersion,
    string Result
);

public sealed record AlarmActionView(
    string ActionId,
    string AlarmId,
    string OwnerId,
    string Status,
    string Content,
    string? Result,
    long Version,
    long AlarmVersion
);

