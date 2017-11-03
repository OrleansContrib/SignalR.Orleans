using System;

namespace OrleansR
{
    public static class Constants
    {
        public const string STORAGE_PROVIDER = "ORLEANS_SIGNALR_STORAGE_PROVIDER";
        public const string SERVERS_STREAM = "SERVERS_STREAM";
        public const string STREAM_PROVIDER = "ORLEANS_SIGNALR_STREAM_PROVIDER";
        public const string CLIENT_DISCONNECT_STREAM = "CLIENT_DISCONNECT_STREAM";
        public static readonly Guid CLIENT_DISCONNECT_STREAM_ID = Guid.Parse("bdcff7e7-3734-48ab-8599-17d915011b85");
        public static readonly Guid ALL_STREAM_ID = Guid.Parse("fbe53ecd-d896-4916-8281-5571d6733566");

    }
}