using System;

namespace SignalR.Orleans.Clients
{
    [Serializable]
    public class ClientMessage
    {
        public string HubName { get; set; }
        public string ConnectionId { get; set; }
        public object Payload { get; set; }
    }
}