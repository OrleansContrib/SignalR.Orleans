using SignalR.Orleans.Clients;
using SignalR.Orleans.ConnectionGroups;

namespace SignalR.Orleans.Core
{
    public class HubContext<THub>
    {
        private readonly IGrainFactory _grainFactory;
        private readonly string _hubName;

        public HubContext(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
            var hubType = typeof(THub);
            _hubName = hubType.IsInterface && hubType.Name.StartsWith("I")
                ? hubType.Name[1..]
                : hubType.Name;
        }

        public IClientGrain Client(string connectionId) => _grainFactory.GetClientGrain(_hubName, connectionId);
        public IConnectionGroupGrain Group(string groupName) => _grainFactory.GetGroupGrain(_hubName, groupName);
        public IConnectionGroupGrain User(string userId) => _grainFactory.GetUserGrain(_hubName, userId);
    }
}