namespace SignalR.Orleans.Clients
{
    // todo: debugger display
    public class ClientMessage
    {
        public string HubName { get; set; }
        public string ConnectionId { get; set; }
        public object Payload { get; set; } // todo: can this be typed InvocationMessage?
    }
}