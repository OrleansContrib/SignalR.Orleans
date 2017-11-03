using System.Collections.Generic;
using System.Threading.Tasks;
using OrleansR.Clients;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace OrleansR.Groups
{
    [StorageProvider(ProviderName = "GroupState")]
    public class GroupGrain : Grain<GroupState>, IGroupGrain
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
                    subscriptionTasks.Add(subscription.ResumeAsync(async (item, token) => await this.RemoveMember(item)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public async Task AddMember(string connectionId)
        {
            if (!this.State.Members.ContainsKey(connectionId))
            {
                var clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
                var subscription = await clientDisconnectStream.SubscribeAsync(async (item, token) => await this.RemoveMember(item));
                this.State.Members.Add(connectionId, subscription);
                await this.WriteStateAsync();
            }
        }

        public async Task RemoveMember(string connectionId)
        {
            await this.State.Members[connectionId]?.UnsubscribeAsync();
            this.State.Members.Remove(connectionId);
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

        public Task SendMessage(object message)
        {
            var tasks = new List<Task>();
            foreach (var member in this.State.Members)
            {
                var client = GrainFactory.GetGrain<IClientGrain>(member.Key);
                tasks.Add(client.SendMessage(message));
            }

            return Task.WhenAll(tasks);
        }
    }

    public class GroupState
    {
        public Dictionary<string, StreamSubscriptionHandle<string>> Members { get; set; } = new Dictionary<string, StreamSubscriptionHandle<string>>();
    }
}