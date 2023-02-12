using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans;

[Immutable, GenerateSerializer]
public sealed record AllMessage([Immutable] InvocationMessage Message, [Immutable] IReadOnlyList<string>? ExcludedIds = null);
