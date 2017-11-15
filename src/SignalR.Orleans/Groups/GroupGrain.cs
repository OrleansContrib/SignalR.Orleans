using Orleans.Providers;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class GroupGrain : ConnectionGroupGrain<GroupState>, IGroupGrain
    {
    }

	public class GroupState : ConnectionGroupState
    {
    }
}