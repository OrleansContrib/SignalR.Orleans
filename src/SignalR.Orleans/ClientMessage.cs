using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;

namespace SignalR.Orleans
{
    [Immutable]
    public sealed record ClientMessage(string HubName, string ConnectionId, Immutable<InvocationMessage> Message);
}