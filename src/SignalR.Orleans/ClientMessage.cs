using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans;

[Immutable, GenerateSerializer]
public sealed record ClientMessage(string HubName, string ConnectionId, [Immutable] InvocationMessage Message);
