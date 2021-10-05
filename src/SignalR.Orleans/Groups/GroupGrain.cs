using Orleans.Runtime;
using Orleans.Concurrency;
using Microsoft.Extensions.Logging;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    [Reentrant]
    internal class GroupGrain : ConnectionGrain<GroupState>, IGroupGrain
    {
        private const string GROUP_STORAGE = "GroupState";
        public GroupGrain(
            ILogger<GroupGrain> logger,
            [PersistentState(GROUP_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<GroupState> groupState)
            : base(logger, groupState)
        {
        }
    }

    internal class GroupState : ConnectionState
    {
    }
}