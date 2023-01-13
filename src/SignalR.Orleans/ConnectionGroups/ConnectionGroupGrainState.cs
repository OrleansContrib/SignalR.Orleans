namespace SignalR.Orleans.ConnectionGroups
{
    internal sealed class ConnectionGroupGrainState
    {
        public HashSet<string> ConnectionIds { get; set; } = new();
    }
}
