using Orleans.Concurrency;

namespace SignalR.Orleans.Core;

/// <summary>
/// Represents an object that can invoke hub methods on a group of connections.
/// </summary>
public interface IHubGroupMessageInvoker : IHubMessageInvoker
{
    /// <summary>
    /// Invokes a method on the hub except the specified connection ids.
    /// </summary>
    /// <param name="methodName">Target method name to invoke.</param>
    /// <param name="args">Arguments to pass to the target method.</param>
    /// <param name="excludedConnectionIds">Connection ids to exclude.</param>
    [ReadOnly] // Allows re-entrancy on this method
    Task SendExcept(string methodName, object?[] args, IEnumerable<string> excludedConnectionIds);
}
