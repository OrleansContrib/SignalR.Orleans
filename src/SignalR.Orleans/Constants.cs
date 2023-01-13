using System;
using Orleans.Streams;

namespace SignalR.Orleans
{
    public static class Constants
    {
        /// <summary>
        /// Name of the streams storage provider used by the signalR orleans backplane grain streams.
        /// </summary>
        public const string PUBSUB_PROVIDER = "ORLEANS_SIGNALR_PUBSUB_PROVIDER";

        /// <summary>
        /// Name of the state storage provider used by signalR orleans backplane grains.
        /// </summary>
        public const string STORAGE_PROVIDER = "ORLEANS_SIGNALR_STORAGE_PROVIDER";

        /// <summary>
        /// Name used to access the <see cref="IStreamProvider"/> that supplies signalR orleans backplane streams.
        /// </summary>
        public const string STREAM_PROVIDER = "ORLEANS_SIGNALR_STREAM_PROVIDER";

        /// <summary>
        /// The number of minutes that each signalR hub server must pulse the server directory.
        /// </summary>
        internal const int SERVER_HEARTBEAT_PULSE_IN_MINUTES = 30;
    }
}