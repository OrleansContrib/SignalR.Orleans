using System.Diagnostics;

namespace SignalR.Orleans.Clients
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class ClientGrainState
    {
        private string DebuggerDisplay => $"ServerId: '{ServerId}'";

        public Guid ServerId { get; set; }
    }
}