using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;
using Orleans.Runtime;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    [Reentrant]
    internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
    {
        private const string GROUP_STORAGE = "GroupState";
        public GroupGrain(
            ILogger<GroupGrain> logger,
            [PersistentState(GROUP_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<GroupState> groupState,
            IOptions<InternalOptions> options)
            : base(logger, groupState, options)
        {
        }
    }

    internal class GroupState : ConnectionState
    {
    }
}