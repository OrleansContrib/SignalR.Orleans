using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.Orleans.Clients
{
    internal record struct ClientKey
    {
        public required string HubType { get; init; }
        public required string ConnectionId { get; init; }

        public string ToGrainPrimaryKey() => $"{HubType}:{ConnectionId}";

        public static ClientKey FromGrainPrimaryKey(string primaryKey)
        {
            var parts = primaryKey.Split(':', 2);
            return new() { HubType = parts[0], ConnectionId = parts[1] };
        }
    }
}