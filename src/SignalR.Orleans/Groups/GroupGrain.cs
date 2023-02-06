using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups;

[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
{
	public GroupGrain(ILogger<GroupGrain> logger) : base(logger)
	{
	}
}

internal class GroupState : ConnectionState
{
}