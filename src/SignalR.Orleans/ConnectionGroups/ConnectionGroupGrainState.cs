namespace SignalR.Orleans.ConnectionGroups
{
    [GenerateSerializer]
    internal sealed class ConnectionGroupGrainState
    {
        [Id(0)]
        public HashSet<string> ConnectionIds { get; set; } = new();
    }
}
