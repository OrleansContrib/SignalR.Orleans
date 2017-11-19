using Orleans;
using Orleans.Streams;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGroupGrain<TGrainState> : Grain<TGrainState>, IConnectionGroupGrain where TGrainState : ConnectionGroupState, new()
    {
        private IStreamProvider _streamProvider;

        public override async Task OnActivateAsync()
        {
            this._streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var member in this.State.Members)
            {
                var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, member.Key);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (connectionId, token) => await this.RemoveMember(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task AddMember(string hubName, string connectionId)
        {
            if (!this.State.Members.ContainsKey(connectionId))
            {
                if (string.IsNullOrWhiteSpace(State.HubName))
                    State.HubName = hubName;

                var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
                var subscription = await clientDisconnectStream.SubscribeAsync(async (connId, token) => await this.RemoveMember(connId));
                this.State.Members.Add(connectionId, subscription);
                await this.WriteStateAsync();
            }
        }

        public virtual async Task RemoveMember(string connectionId)
        {
            if (State.Members.ContainsKey(connectionId))
            {
                await this.State.Members[connectionId].UnsubscribeAsync();
                this.State.Members.Remove(connectionId);
            }
            if (this.State.Members.Count == 0)
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
            foreach (var member in this.State.Members)
            {
                var client = GrainFactory.GetClientGrain(State.HubName, member.Key);
                tasks.Add(client.SendMessage(message));
            }

            return Task.WhenAll(tasks);
        }

        public Task<int> Count()
        {
            return Task.FromResult(State.Members.Count);
        }
    }

    internal abstract class ConnectionGroupState
    {
        public Dictionary<string, StreamSubscriptionHandle<string>> Members { get; set; } = new Dictionary<string, StreamSubscriptionHandle<string>>();
        public string HubName { get; set; }
    }
}