using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups;

/// <summary>
/// Group of connections by hub and a custom group name e.g. '{hubName}:{groupName}' ('hero:top')
/// </summary>
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