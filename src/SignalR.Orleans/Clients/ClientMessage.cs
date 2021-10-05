using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans.Clients
{
    public record ClientMessage(string HubName, string ConnectionId, InvocationMessage Payload);
}