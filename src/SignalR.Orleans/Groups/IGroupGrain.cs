using System;
using System.Threading.Tasks;
using Orleans;

namespace SignalR.Orleans.Groups
{
    public interface IGroupGrain : IGrainWithStringKey
    {
        Task AddMember(string connectionId);
        Task RemoveMember(string connectionId);
        Task SendMessage(object message);
    }
}