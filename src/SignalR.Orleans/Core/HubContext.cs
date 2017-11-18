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

        public IUserGrain User(string key) => _grainFactory.GetGrain<IUserGrain>(Utils.BuildGrainName(_hubTypeName, key));
        public IGroupGrain Group(string key) => _grainFactory.GetGrain<IGroupGrain>(Utils.BuildGrainName(_hubTypeName, key));
        public IClientGrain Client(string key) => _grainFactory.GetGrain<IClientGrain>(Utils.BuildGrainName(_hubTypeName, key));
    }
}