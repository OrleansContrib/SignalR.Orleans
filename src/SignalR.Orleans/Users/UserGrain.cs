using Orleans.Providers;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users
{
    [StorageProvider(ProviderName = Constants.GrainPersistence)]
    internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
    {
    }

    internal class UserState : ConnectionState
    {
    }
}