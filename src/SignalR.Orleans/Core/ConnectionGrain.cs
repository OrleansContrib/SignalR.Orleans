using System.Linq;
using System.Buffers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans;
using Orleans.Streams;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGrain<TGrainState> : Grain, IConnectionGrain
        where TGrainState : ConnectionState, new()
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<TGrainState> _connectionState;
        private readonly Dictionary<string, StreamSubscriptionHandle<string>> _connectionStreamHandles = new();
        private IStreamProvider _streamProvider = default!;

        protected ConnectionGrainKey KeyData;

        internal ConnectionGrain(
            ILogger logger,
            IPersistentState<TGrainState> connectionState)
        {
            _logger = logger;
            _connectionState = connectionState;
        }

        public override async Task OnActivateAsync()
        {
            KeyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var connection in _connectionState.State.Connections)
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connection);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync((connectionId, _) => Remove(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task Add(string connectionId)
        {
            var shouldWriteState = _connectionState.State.Connections.Add(connectionId);
            if (!_connectionStreamHandles.ContainsKey(connectionId))
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
                var subscription = await clientDisconnectStream.SubscribeAsync((connId, _) => Remove(connId));
                _connectionStreamHandles[connectionId] = subscription;
            }

            if (shouldWriteState)
                await _connectionState.WriteStateAsync();
        }

        public virtual async Task Remove(string connectionId)
        {
            var shouldWriteState = _connectionState.State.Connections.Remove(connectionId);
            if (_connectionStreamHandles.TryGetValue(connectionId, out var stream))
            {
                await stream.UnsubscribeAsync();
                _connectionStreamHandles.Remove(connectionId);
            }

            if (_connectionState.State.Connections.Count == 0)
            {
                await _connectionState.ClearStateAsync();
                DeactivateOnIdle();
            }
            else if (shouldWriteState)
            {
                await _connectionState.WriteStateAsync();
            }
        }

        public virtual Task Send(Immutable<InvocationMessage> message) => SendAll(message, _connectionState.State.Connections);

        public Task SendExcept(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var message = new Immutable<InvocationMessage>(new InvocationMessage(methodName, args));
            return SendAll(message, _connectionState.State.Connections.Where(x => !excludedConnectionIds.Contains(x)).ToList());
        }

        public Task<int> Count() => Task.FromResult(_connectionState.State.Connections.Count);

        protected Task SendAll(Immutable<InvocationMessage> message, IReadOnlyCollection<string> connections)
        {
            _logger.LogDebug("Sending message to {hubName}.{targetMethod} on group {groupId} to {connectionsCount} connection(s)",
                KeyData.HubName, message.Value.Target, KeyData.Id, connections.Count);

            var tasks = ArrayPool<Task>.Shared.Rent(connections.Count);
            try
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    var connection = connections.ElementAt(i);
                    var client = GrainFactory.GetClientGrain(KeyData.HubName, connection);
                    tasks[i] = client.Send(message);
                }

                return Task.WhenAll(tasks.Where(x => x != null).ToArray());
            }
            finally
            {
                ArrayPool<Task>.Shared.Return(tasks);
            }
        }
    }

    internal abstract class ConnectionState
    {
        public HashSet<string> Connections { get; set; } = new();
    }
}