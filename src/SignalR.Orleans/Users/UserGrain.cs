using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Concurrency;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users
{
    [Reentrant]
    internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
    {
        private const string USER_STORAGE = "UserState";
        public UserGrain(
            ILogger<UserGrain> logger,
            [PersistentState(USER_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<UserState> userState)
            : base(logger, userState)
        {
        }
    }

    internal class UserState : ConnectionState
    {
    }
}