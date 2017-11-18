using Orleans;
using Orleans.Streams;
using SignalR.Orleans.Clients;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGroupGrain<TGrainState> : Grain<TGrainState>, IConnectionGroupGrain where TGrainState : ConnectionGroupState, new()
    {
        private IStreamProvider _streamProvider;
        private StreamSubscriptionHandle<string>[] _subscriptions;

        public override async Task OnActivateAsync()
        {
            this._streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, Constants.CLIENT_DISCONNECT_STREAM);

            var subscriptionTasks = new List<Task<StreamSubscriptionHandle<string>>>();
            var subscriptionHandles = await clientDisconnectStream.GetAllSubscriptionHandles();
            if (subscriptionHandles != null && subscriptionHandles.Count > 0)
            {
                foreach (var subscription in subscriptionHandles)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (item, token) => await this.RemoveMember(item)));
                }
            }
            else
            {
                subscriptionTasks.Add(clientDisconnectStream.SubscribeAsync((item, token) => this.RemoveMember(item)));
            }
            _subscriptions = await Task.WhenAll(subscriptionTasks);
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
                var tasks = _subscriptions.Select(subscription => subscription.UnsubscribeAsync()).ToList();
                await Task.WhenAll(tasks);
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