using Orleans;
using System;
using System.Threading.Tasks;

namespace SignalR.Orleans.Clients
{
    public interface IClientGrain : IGrainWithStringKey
    {
        Task SendMessage(object message);
        Task OnConnect(Guid serverId, string hubName, string connectionId);
        Task OnDisconnect();
    }
}