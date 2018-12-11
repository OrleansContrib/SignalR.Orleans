using System;

namespace SignalR.Orleans
{
    public static class Constants
    {
        public const string PubSub = "PubSubStore";
        public const string GrainPersistence = "ORLEANS_SIGNALR_STORAGE_PROVIDER";
        public const string ServersStream = "SERVERS_STREAM";
        public const string StreamProvider = "ORLEANS_SIGNALR_STREAM_PROVIDER";
        public static readonly Guid ClientDisconnectStreamId = Guid.Parse("bdcff7e7-3734-48ab-8599-17d915011b85");
        public static readonly Guid AllStreamId = Guid.Parse("fbe53ecd-d896-4916-8281-5571d6733566");
    }
}