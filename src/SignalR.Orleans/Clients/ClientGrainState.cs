using System.Diagnostics;

namespace SignalR.Orleans.Clients
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [GenerateSerializer]
    internal sealed class ClientGrainState
    {
        private string DebuggerDisplay => $"ServerId: '{ServerId}'";

        [Id(0)]
        public Guid ServerId { get; set; }
    }
}