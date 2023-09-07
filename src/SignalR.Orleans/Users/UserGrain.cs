using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Users
{
    [Reentrant]
    internal class UserGrain : ConnectionGrain<UserState>, IUserGrain
    {
        private const string USER_STORAGE = "UserState";
        public UserGrain(
            ILogger<UserGrain> logger,
            [PersistentState(USER_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<UserState> userState,
            IOptions<InternalOptions> options)
            : base(logger, userState, options)
        {
        }
    }

    internal class UserState : ConnectionState
    {
    }
}