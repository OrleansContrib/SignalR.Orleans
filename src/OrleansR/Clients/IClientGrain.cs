using System;
using System.Threading.Tasks;
using Orleans;

namespace OrleansR.Clients
{
    public interface IClientGrain : IGrainWithStringKey
    {
        Task SendMessage(object message);
        Task OnConnect(Guid serverId);
        Task OnDisconnect();
    }
}