using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users;

[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
{
	public UserGrain(ILogger<UserGrain> logger) : base(logger)
	{
	}
}

internal class UserState : ConnectionState
{
}