using Orleans;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Users;

namespace SignalR.Orleans.Core
{
    public class HubContext<THub>
    {
        private readonly IGrainFactory _grainFactory;
        private readonly string _hubTypeName;

        public HubContext(IGrainFactory grain)
        {
            _grainFactory = grain;
            var hubType = typeof(THub);
            _hubTypeName = hubType.IsInterface && hubType.Name.StartsWith("I")
                ? hubType.Name.Substring(1)
                : hubType.Name;
        }

        public IClientGrain Client(string connectionId) => _grainFactory.GetClientGrain(_hubTypeName, connectionId);
        public IGroupGrain Group(string groupName) => _grainFactory.GetGroupGrain(_hubTypeName, groupName);
        public IUserGrain User(string userId) => _grainFactory.GetUserGrain(_hubTypeName, userId);

    }
}