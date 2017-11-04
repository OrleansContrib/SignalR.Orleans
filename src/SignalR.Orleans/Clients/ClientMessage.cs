namespace SignalR.Orleans.Clients
{
    public class ClientMessage
    {
        public string ConnectionId { get; set; }
        public object Payload { get; set; }
    }
}