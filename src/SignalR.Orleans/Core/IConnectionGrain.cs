using Orleans;
using System.Threading.Tasks;

namespace SignalR.Orleans.Core
{
    public interface IConnectionGrain : IGrainWithStringKey
    {
        Task Add(string connectionId);
        Task Remove(string connectionId);
        Task SendMessage(object message);
    }
}