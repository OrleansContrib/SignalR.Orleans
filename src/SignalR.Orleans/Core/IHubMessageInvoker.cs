using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace SignalR.Orleans.Core
{
    public interface IHubMessageInvoker : IAddressable
    {
        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="message">Message to invoke.</param>
        Task Send(Immutable<InvocationMessage> message);
    }
}