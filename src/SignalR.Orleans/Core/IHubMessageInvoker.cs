using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace SignalR.Orleans.Core
{
    /// <summary>
    /// Represents an object that can invoke hub methods.
    /// </summary>
    public interface IHubMessageInvoker : IAddressable
    {
        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="message">Message to invoke.</param>
        Task Send(Immutable<InvocationMessage> message);

        // TODO: Implement this
        // [OneWay]
        // Task SendOneWay(Immutable<InvocationMessage> message);
    }
}