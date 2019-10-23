using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGrain<TGrainState> : Grain<TGrainState>, IConnectionGrain
        where TGrainState : ConnectionState, new()
    {
        private readonly ILogger _logger;
        private IStreamProvider _streamProvider;
        private Dictionary<string, StreamSubscriptionHandle<string>> _connectionStreamHandles;

        protected ConnectionGrainKey KeyData;

        internal ConnectionGrain(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            KeyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _connectionStreamHandles = new Dictionary<string, StreamSubscriptionHandle<string>>();
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connection);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (connectionId, _) => await Remove(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task Add(string connectionId)
        {
            var shouldWriteState = State.Connections.Add(connectionId);
            if (!_connectionStreamHandles.ContainsKey(connectionId))
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
                var subscription = await clientDisconnectStream.SubscribeAsync(async (connId, _) => await Remove(connId));
                _connectionStreamHandles[connectionId] = subscription;
            }

            if (shouldWriteState)
                await WriteStateAsync();
        }

        public virtual async Task Remove(string connectionId)
        {
            var shouldWriteState = State.Connections.Remove(connectionId);
            if (_connectionStreamHandles.TryGetValue(connectionId, out var stream))
            {
                await stream.UnsubscribeAsync();
                _connectionStreamHandles.Remove(connectionId);
            }

            if (State.Connections.Count == 0)
            {
                await ClearStateAsync();
                DeactivateOnIdle();
            }
            else if (shouldWriteState)
            {
                await WriteStateAsync();
            }
        }

        public virtual Task Send(Immutable<InvocationMessage> message)
        {
            return SendAll(message, State.Connections);
        }

        public Task SendExcept(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var message = new Immutable<InvocationMessage>(new InvocationMessage(methodName, args));
            return SendAll(message, State.Connections.Where(x => !excludedConnectionIds.Contains(x)).ToList());
        }

        public Task<int> Count()
        {
            return Task.FromResult(State.Connections.Count);
        }

        protected Task SendAll(Immutable<InvocationMessage> message, IReadOnlyCollection<string> connections)
        {
            _logger.LogDebug("Sending message to {hubName}.{targetMethod} on group {groupId} to {connectionsCount} connection(s)",
                KeyData.HubName, message.Value.Target, KeyData.Id, connections.Count);

            var tasks = ArrayPool<Task>.Shared.Rent(connections.Count);
            try
            {
                int index = 0;
                foreach (var connection in connections)
                {
                    var client = GrainFactory.GetClientGrain(KeyData.HubName, connection);
                    tasks[index++] = client.Send(message);
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
        public HashSet<string> Connections { get; set; } = new HashSet<string>();
    }
}