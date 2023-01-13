namespace SignalR.Orleans.Core
{
    public sealed class ServerDirectoryState
    {
        public Dictionary<Guid, DateTime> ServerHeartBeats { get; set; } = new();
    }
}