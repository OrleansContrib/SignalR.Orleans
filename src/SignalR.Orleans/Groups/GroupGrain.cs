using Orleans.Providers;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
    {
    }

    internal class GroupState : ConnectionState
    {
    }
}