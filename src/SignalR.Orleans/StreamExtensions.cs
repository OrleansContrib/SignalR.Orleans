using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Orleans
{
    // todo: move to Stream Utils?
    internal static class StreamReplicaExtensions
    {
        public static string BuildReplicaStreamName(string streamId, int replicaIndex)
            => $"{streamId}:{replicaIndex}";
        public static string BuildReplicaStreamName(Guid streamId, int replicaIndex)
            => BuildReplicaStreamName(streamId.ToString(), replicaIndex);

        private static readonly Random Randomizer = new Random();

        /// <summary>
        /// Get stream sharded by replicas randomly according to the size given.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamProvider"></param>
        /// <param name="streamId"></param>
        /// <param name="streamNamespace"></param>
        /// <param name="replicas">Max replicas to obtain stream from.</param>
        /// <returns></returns>
        public static IAsyncStream<T> GetStreamReplicaRandom<T>(this IStreamProvider streamProvider, Guid streamId, string streamNamespace, int replicas)
            => streamProvider.GetStream<T>(BuildReplicaStreamName(streamId, Randomizer.Next(0, replicas)), streamNamespace);

        public static async Task ResumeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream, Func<T, StreamSequenceToken, Task> onNextAsync)
        {
            var subscriptions = await stream.GetAllSubscriptionHandles();
            if (subscriptions?.Count > 0)
            {
                var tasks = subscriptions.Select(x => x.ResumeAsync(onNextAsync));
                await Task.WhenAll(tasks);
            }
        }

        public static async Task UnsubscribeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream)
        {
            var subscriptions = await stream.GetAllSubscriptionHandles();
            if (subscriptions?.Count > 0)
            {
                var tasks = subscriptions.Select(x => x.UnsubscribeAsync());
                await Task.WhenAll(tasks);
            }
        }
    }

    /// <summary>
    /// Contains streams replicated for sharding used for listening and managing multiple replicas easier.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class StreamReplicaContainer<T>
    {
        protected string DebuggerDisplay => $"StreamId: '{StreamId}', StreamNamespace: '{StreamNamespace}', MaxReplicas: {MaxReplicas}";

        public string StreamId { get; }
        public string StreamNamespace { get; }
        public int MaxReplicas { get; }

        private readonly List<IAsyncStream<T>> _streams = new List<IAsyncStream<T>>();

        /// <summary>
        /// Create a new instance of stream with replicas.
        /// </summary>
        /// <param name="streamProvider"></param>
        /// <param name="streamId"></param>
        /// <param name="streamNamespace"></param>
        /// <param name="maxReplicas">Max replicas to create.</param>
        public StreamReplicaContainer(IStreamProvider streamProvider, string streamId, string streamNamespace, int maxReplicas)
        {
            StreamId = streamId;
            StreamNamespace = streamNamespace;
            MaxReplicas = maxReplicas;

            for (int i = 0; i < maxReplicas; i++)
            {
                var streamIdReplica = StreamReplicaExtensions.BuildReplicaStreamName(streamId, i);
                _streams.Add(streamProvider.GetStream<T>(streamIdReplica, streamNamespace));
            }
        }

        public StreamReplicaContainer(IStreamProvider streamProvider, Guid streamId, string streamNamespace,
            int maxReplicas) : this(streamProvider, streamId.ToString(), streamNamespace, maxReplicas)
        {

        }

        public async Task SubscribeAsync(Func<T, StreamSequenceToken, Task> onNextAsync)
        {
            var tasks = new List<Task>(MaxReplicas);

            foreach (var stream in _streams)
                tasks.Add(stream.SubscribeAsync(onNextAsync));

            await Task.WhenAll(tasks);
        }

        public async Task<IList<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles()
        {
            var tasks = new List<Task<IList<StreamSubscriptionHandle<T>>>>(MaxReplicas);

            foreach (var stream in _streams)
                tasks.Add(stream.GetAllSubscriptionHandles());

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(x => x).ToList();
        }
    }
}