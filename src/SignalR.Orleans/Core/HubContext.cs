using Orleans;
using SignalR.Orleans.Users;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Clients;

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
        public IGroupGrain Group(string groupName) => _grainFactory.GetGroupGrain(_hubName, groupName);
        public IUserGrain User(string userId) => _grainFactory.GetUserGrain(_hubName, userId);
    }
}