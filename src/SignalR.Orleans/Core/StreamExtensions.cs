using Orleans.Runtime;
using Orleans.Streams;
using SignalR.Orleans;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Gets the OrleansSignalR stream provider.
        /// </summary>
        public static IStreamProvider GetOrleansSignalRStreamProvider(this IClusterClient client)
            => client.GetStreamProvider(SignalROrleansConstants.SIGNALR_ORLEANS_STREAM_PROVIDER);

        /// <summary>
        /// Gets the OrleansSignalR stream provider.
        /// </summary>
        public static IStreamProvider GetOrleansSignalRStreamProvider(this IGrainBase grain)
            => grain.GetStreamProvider(SignalROrleansConstants.SIGNALR_ORLEANS_STREAM_PROVIDER);

        /// <summary>
        ///  Gets a stream that you can listen on to receive the server disconnection (silo shutdown) event
        /// </summary>
        /// <param name="serverId">The id of ther server that has shutdown/disconnected.</param>
        public static IAsyncStream<Guid> GetServerDisconnectionStream(this IStreamProvider streamProvider, Guid serverId)
            => streamProvider.GetStream<Guid>(StreamId.Create("SERVER_DISCONNECT_STREAM", serverId));

        /// <summary>
        /// Gets a stream that you can use to write messages that need to be sent to the connection that is connected at that server.
        /// </summary>
        /// <param name="serverId">The id of the server that the message destination connection is connected at.</param>
        public static IAsyncStream<ClientMessage> GetServerStream(this IStreamProvider streamProvider, Guid serverId)
            => streamProvider.GetStream<ClientMessage>(StreamId.Create("SERVER_STREAM", serverId));

        /// <summary>
        /// Gets a stream that you can listen on to receive a message when the connection with the given <paramref name="connectionId"/> has been disconnected.
        /// </summary>
        /// <param name="connectionId">The id of the connection that we are listening for.</param>
        public static IAsyncStream<string> GetClientDisconnectionStream(this IStreamProvider streamProvider, string connectionId)
          => streamProvider.GetStream<string>(StreamId.Create("CLIENT_DISCONNECT_STREAM", connectionId));

        /// <summary>
        /// Gets a stream that sends a message to all clients connected to a given hub.
        /// </summary>
        /// <param name="hubName">The name of the hub type that the message will be sent to.</param>
        public static IAsyncStream<AllMessage> GetAllStream(this IStreamProvider streamProvider, string hubName)
            => streamProvider.GetStream<AllMessage>(StreamId.Create("ALL_STREAM", hubName));
    }

}