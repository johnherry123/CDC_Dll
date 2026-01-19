using System.Data;

namespace DeviceService.Core.Abstractions.Protocol;

public readonly record struct ConnectionHealthSnapshot
(
    ConnectionState State,
    int consecutiveTimeouts,
    TimeSpan LastSeenAge,
    string? LastError

);