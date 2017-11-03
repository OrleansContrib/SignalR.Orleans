using System;
using System.Threading.Tasks;
using Orleans;

namespace OrleansR.Groups
{
    public interface IGroupGrain : IGrainWithStringKey
    {
        Task AddMember(string connectionId);
        Task RemoveMember(string connectionId);
        Task SendMessage(object message);
    }
}