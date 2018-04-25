using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGrain<TGrainState> : Grain<TGrainState>, IConnectionGrain where TGrainState : ConnectionState, new()
    {
        private IStreamProvider _streamProvider;

        public override async Task OnActivateAsync()
        {
            this._streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var connection in this.State.Connections)
            {
                var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connection.Key);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (connectionId, token) => await this.Remove(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task Add(string hubName, string connectionId)
        {
            if (!this.State.Connections.ContainsKey(connectionId))
            {
                if (string.IsNullOrWhiteSpace(State.HubName))
                    State.HubName = hubName;

                var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
                var subscription = await clientDisconnectStream.SubscribeAsync(async (connId, token) => await this.Remove(connId));
                this.State.Connections.Add(connectionId, subscription);
                await this.WriteStateAsync();
            }
        }

        public virtual async Task Remove(string connectionId)
        {
            if (State.Connections.ContainsKey(connectionId))
            {
                await this.State.Connections[connectionId].UnsubscribeAsync();
                this.State.Connections.Remove(connectionId);
            }
            if (this.State.Connections.Count == 0)
            {
                await this.ClearStateAsync();
                this.DeactivateOnIdle();
            }
            else
            {
                await this.WriteStateAsync();
            }
        }

        public virtual Task SendMessage(object message)
        {
            var tasks = new List<Task>();
            foreach (var connection in this.State.Connections)
            {
                var client = GrainFactory.GetClientGrain(State.HubName, connection.Key);
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
        public string HubName { get; set; }
    }
}