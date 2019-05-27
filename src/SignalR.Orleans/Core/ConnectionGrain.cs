using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGrain<TGrainState> : Grain<TGrainState>, IConnectionGrain
        where TGrainState : ConnectionState, new()
    {
        protected ConnectionGrainKey KeyData;
        private IStreamProvider _streamProvider;

        public override async Task OnActivateAsync()
        {
            KeyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connection.Key);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (connectionId, token) => await Remove(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task Add(string connectionId)
        {
            if (State.Connections.ContainsKey(connectionId))
                return;

            var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
            var subscription = await clientDisconnectStream.SubscribeAsync(async (connId, token) => await Remove(connId));
            State.Connections.Add(connectionId, subscription);
            await WriteStateAsync();
        }

        public virtual async Task Remove(string connectionId)
        {
            if (State.Connections.TryGetValue(connectionId, out var stream))
            {
                await stream.UnsubscribeAsync();
                State.Connections.Remove(connectionId);
            }
            if (State.Connections.Count == 0)
            {
                await ClearStateAsync();
                DeactivateOnIdle();
            }
            else
            {
                await WriteStateAsync();
            }
        }

        public virtual Task SendMessage(object message)
        {
            var tasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                var client = GrainFactory.GetClientGrain(KeyData.HubName, connection.Key);
                tasks.Add(client.SendMessage(message));
            }

            return Task.WhenAll(tasks);
        }

        public Task<int> Count()
        {
            return Task.FromResult(State.Connections.Count);
        }
    }

    internal abstract class ConnectionState
    {
        public Dictionary<string, StreamSubscriptionHandle<string>> Connections { get; set; } = new Dictionary<string, StreamSubscriptionHandle<string>>();
    }
}