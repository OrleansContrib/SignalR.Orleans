using SignalR.Orleans.Core;

namespace SignalR.Orleans.Clients;

public interface IClientGrain : IHubMessageInvoker, IGrainWithStringKey
{
	Task OnConnect(Guid serverId);
	Task OnDisconnect(string reason = null);
}