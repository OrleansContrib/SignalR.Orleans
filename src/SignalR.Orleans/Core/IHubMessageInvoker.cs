using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans.Core
{
    public interface IHubMessageInvoker
    {
        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="message">Message to invoke.</param>
        Task Send(InvocationMessage message);
    }
}