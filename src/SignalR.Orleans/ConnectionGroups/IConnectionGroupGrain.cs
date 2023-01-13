using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.ConnectionGroups
{
    public interface IConnectionGroupGrain : IGrainWithStringKey, IHubMessageInvoker
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
        Task<int> Count();

        /// <summary>
        /// Invokes a method on the hub except the specified connection ids.
        /// </summary>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        /// <param name="excludedConnectionIds">Connection ids to exclude.</param>
        Task SendExcept(string methodName, object?[] args, IEnumerable<string> excludedConnectionIds);
    }
}
