using SignalR.Orleans.Clients;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Users;

namespace SignalR.Orleans.Core;

public class HubContext<THub> : HubContext
{
	public HubContext(IGrainFactory grainFactory) : base(grainFactory, GetHubName())
	{
	}

	public static string GetHubName()
	{
		var hubType = typeof(THub);
		return hubType.IsInterface && hubType.Name.StartsWith("I")
			? hubType.Name.Substring(1)
			: hubType.Name;
	}
}

public class HubContext
{
	private readonly IGrainFactory _grainFactory;

	public string HubName { get; init; }

	public HubContext(IGrainFactory grainFactory, string hubName)
	{
		_grainFactory = grainFactory;
		HubName = hubName;
	}

	public IClientGrain Client(string connectionId) => _grainFactory.GetClientGrain(HubName, connectionId);
	public IGroupGrain Group(string groupName) => _grainFactory.GetGroupGrain(HubName, groupName);
	public IUserGrain User(string userId) => _grainFactory.GetUserGrain(HubName, userId);
}
