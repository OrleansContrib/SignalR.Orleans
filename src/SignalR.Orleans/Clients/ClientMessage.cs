using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans.Clients
{
    // todo: debugger display
    public class ClientMessage
    {
        public string HubName { get; set; }
        public string ConnectionId { get; set; }
        public InvocationMessage Payload { get; set; }
    }
}