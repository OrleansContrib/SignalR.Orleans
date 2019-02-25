using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
    {
        public Task SendMessageExcept(object message, IReadOnlyList<string> excludedIds)
        {
            var tasks = new List<Task>();
            foreach (var connection in this.State.Connections)
            {
                if (excludedIds.Contains(connection.Key)) continue;

                var client = GrainFactory.GetClientGrain(State.HubName, connection.Key);
                tasks.Add(client.SendMessage(message));
            }

            return Task.WhenAll(tasks);
        }
    }

    internal class GroupState : ConnectionState
    {
    }
}