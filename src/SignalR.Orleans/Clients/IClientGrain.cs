using SignalR.Orleans.Core;

namespace SignalR.Orleans.Clients
{
    /// <summary>
    /// A single connection
    /// </summary>
    public interface IClientGrain : IHubMessageInvoker, IGrainWithStringKey
    {
        Task OnConnect(Guid serverId);
        Task OnDisconnect(string? reason = null);
    }
}