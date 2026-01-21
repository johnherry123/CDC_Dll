

using CDC_Dll.Core.Domain.Models;

namespace CDC_Dll.Core.Abstractions.Protocol;

public readonly record struct ConnectionHealthSnapshot
(
    ConnectionState State,
    int consecutiveTimeouts,
    TimeSpan LastSeenAge,
    string? LastError

);