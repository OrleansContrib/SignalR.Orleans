using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace SignalR.Orleans.Core;

/// <summary>
/// Represents an object that can invoke hub methods on a single connection.
/// </summary>
public interface IHubMessageInvoker : IAddressable
{
    /// <summary>
    /// Invokes a method on the hub.
    /// </summary>
    /// <param name="message">Message to invoke.</param>
    [ReadOnly] // Allows re-entrancy on this method
    Task Send(InvocationMessage message);

    [OneWay]
    Task SendOneWay(InvocationMessage message);
}
