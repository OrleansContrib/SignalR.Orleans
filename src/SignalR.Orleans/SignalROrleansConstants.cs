using System;
using Orleans.Streams;

namespace SignalR.Orleans
{
    public static class SignalROrleansConstants
    {
        /// <summary>
        /// Name of the streams storage provider used by the signalR orleans backplane grain streams.
        /// </summary>
        public const string PUBSUB_STORAGE_PROVIDER = "PubSubStore";

        /// <summary>
        /// Name of the state storage provider used by signalR orleans backplane grains.
        /// </summary>
        public const string SIGNALR_ORLEANS_STORAGE_PROVIDER = nameof(SIGNALR_ORLEANS_STORAGE_PROVIDER);

        /// <summary>
        /// Name used to access the <see cref="IStreamProvider"/> that supplies signalR orleans backplane streams.
        /// </summary>
        public const string SIGNALR_ORLEANS_STREAM_PROVIDER = nameof(SIGNALR_ORLEANS_STREAM_PROVIDER);

        /// <summary>
        /// The number of minutes that each signalR hub server must heartbeat the server directory grain.
        /// There's just one server directory grain (id 0) per cluster.
        /// </summary>
        internal const int SERVER_HEARTBEAT_PULSE_IN_MINUTES = 30;
    }
}