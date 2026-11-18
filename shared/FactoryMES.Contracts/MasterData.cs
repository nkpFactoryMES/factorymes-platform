namespace FactoryMES.Contracts;

public sealed record LineChoice(
    string LineId,
    string Code,
    string Name,
    bool Active,
    long Version
);

public sealed record ProductCreate(
    string Code,
    string Name,
    string Unit,
    bool Active
);

public sealed record ProductUpdate(
    long ExpectedVersion,
    string Code,
    string Name,
    string Unit,
    bool Active
);

public sealed record LineCreate(
    string Code,
    string Name,
    bool Active
);

public sealed record LineUpdate(
    long ExpectedVersion,
    string Code,
    string Name,
    bool Active
);

public sealed record EquipmentCreate(
    string Code,
    string Name,
    string LineId,
    bool Active
);

public sealed record EquipmentUpdate(
    long ExpectedVersion,
    string Code,
    string Name,
    string LineId,
    bool Active
);

public sealed record UserCreate(
    string Login,
    string Name,
    string Role,
    string? Email,
    string InitialPassword
);

public sealed record UserUpdate(
    long ExpectedVersion,
    string Name,
    string[] Roles,
    string? Email,
    bool Active
);

public sealed record OrderDraftUpdate(
    long ExpectedVersion,
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

