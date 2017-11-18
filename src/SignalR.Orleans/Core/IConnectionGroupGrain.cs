using Orleans;
using System.Threading.Tasks;

namespace SignalR.Orleans.Core
{
    public interface IConnectionGroupGrain : IGrainWithStringKey
    {
        Task AddMember(string hubName, string connectionId);
        Task RemoveMember(string connectionId);
        Task SendMessage(object message);
        Task<int> Count();
    }
}