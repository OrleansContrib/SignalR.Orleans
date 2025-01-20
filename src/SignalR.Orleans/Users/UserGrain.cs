using Microsoft.AspNetCore.SignalR;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users;

/// <summary>
/// Group of connections by the authenticated user (<see cref="HubConnectionContext.UserIdentifier"/>) e.g. '{hubName}:{userId}' ('hero:xyz')
/// </summary>
[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
{
	public UserGrain(ILogger<UserGrain> logger) : base(logger)
	{
	}
}

[GenerateSerializer]
internal class UserState : ConnectionState
{
}