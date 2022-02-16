using Microsoft.AspNetCore.SignalR;
using Orleans;
using SignalR.Orleans.Users;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Clients;

namespace SignalR.Orleans.Core
{
    public class HubContext<THub> where THub : Hub
    {
        private readonly IGrainFactory _grainFactory;
        private readonly string _hubName;

        public HubContext(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
            _hubName = HubUtility.GetHubName<THub>();
        }

        public IClientGrain Client(string connectionId) => _grainFactory.GetClientGrain(_hubName, connectionId);
        public IGroupGrain Group(string groupName) => _grainFactory.GetGroupGrain(_hubName, groupName);
        public IUserGrain User(string userId) => _grainFactory.GetUserGrain(_hubName, userId);
    }
}