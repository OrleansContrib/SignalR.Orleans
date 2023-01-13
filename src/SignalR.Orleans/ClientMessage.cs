using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;

namespace SignalR.Orleans
{
    [Immutable, GenerateSerializer]
    public sealed record ClientMessage(string HubName, string ConnectionId, Immutable<InvocationMessage> Message);
}