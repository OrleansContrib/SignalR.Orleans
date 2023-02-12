using Orleans.Concurrency;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.ConnectionGroups;

public interface IConnectionGroupGrain : IGrainWithStringKey, IHubGroupMessageInvoker
{
    /// <summary>
    /// Add connection id to the group.
    /// </summary>
    /// <param name="connectionId">Connection id to add.</param>
    Task Add(string connectionId);

    /// <summary>
    /// Remove the connection id from the group.
    /// </summary>
    /// <param name="connectionId">Connection id to remove.</param>
    Task Remove(string connectionId);

    /// <summary>
    /// Gets the connection count of the group.
    /// </summary>
    [ReadOnly] // Allows re-entrancy on this method
    Task<int> Count();
}
