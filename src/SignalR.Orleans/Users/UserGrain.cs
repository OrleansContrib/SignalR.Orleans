using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Providers;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users
{
    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    [Reentrant]
    internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
    {
        public UserGrain(ILogger<UserGrain> logger) : base(logger)
        {
        }
    }

    internal class UserState : ConnectionState
    {
    }
}