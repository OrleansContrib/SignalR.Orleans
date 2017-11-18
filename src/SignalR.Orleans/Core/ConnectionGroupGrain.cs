using Orleans;
using Orleans.Streams;
using SignalR.Orleans.Clients;
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
            var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, Constants.CLIENT_DISCONNECT_STREAM);

            var subscriptionTasks = new List<Task>();
            var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
            if (subscriptions != null && subscriptions.Count > 0)
            {
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (item, token) => await this.RemoveMember(item)));
                }
            }
            else
            {
                subscriptionTasks.Add(clientDisconnectStream.SubscribeAsync((item, token) => this.RemoveMember(item)));
            }

            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task AddMember(string hubName, string connectionId)
        {
            if (!this.State.Members.Contains(connectionId))
            {
                if (string.IsNullOrWhiteSpace(State.HubName))
                {
                    State.HubName = hubName;
                }
                this.State.Members.Add(connectionId);
                await this.WriteStateAsync();
            }
        }

        public virtual async Task RemoveMember(string connectionId)
        {
            if (State.Members.Contains(connectionId))
            {
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
                var client = GrainFactory.GetGrain<IClientGrain>(Utils.BuildGrainName(State.HubName, member));
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
        public HashSet<string> Members { get; set; } = new HashSet<string>();
        public string HubName { get; set; }
    }
}