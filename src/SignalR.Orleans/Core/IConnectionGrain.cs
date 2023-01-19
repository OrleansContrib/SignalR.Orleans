using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;

namespace SignalR.Orleans.Core
{
    /// <summary>
    /// Grain interface Grouped of connections, such as user or custom group.
    /// </summary>
    public interface IConnectionGrain : IHubMessageInvoker, IGrainWithStringKey
    {
        /// <summary>
        /// Add connection id to the group.
        /// </summary>
        /// <param name="connectionId">Connection id to add.</param>
        Task Add(string connectionId);

        /// <summary>
        /// Remove the connection id to the group.
        /// </summary>
        /// <param name="connectionId">Connection id to remove.</param>
        Task Remove(string connectionId);

        /// <summary>
        /// Gets the connection count in the group.
        /// </summary>
        Task<int> Count();

        /// <summary>
        /// Invokes a method on the hub except the specified connection ids.
        /// </summary>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        /// <param name="excludedConnectionIds">Connection ids to exclude.</param>
        Task SendExcept(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds);
    }
}