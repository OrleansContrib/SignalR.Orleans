using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Groups
{
    public interface IGroupGrain : IConnectionGrain
    {
        Task SendMessageExcept(object message, IReadOnlyList<string> excludedIds);
    }
}