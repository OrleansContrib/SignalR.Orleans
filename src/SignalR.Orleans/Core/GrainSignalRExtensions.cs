using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using SignalR.Orleans.Core;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class GrainSignalRExtensions
    {
        public static async Task SendSignalRMessage(this IConnectionGroupGrain grain, string methodName, params object[] message)
        {
            var invocationMessage = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: message);
            await grain.SendMessage(invocationMessage);
        }

        public static HubContext<THub> GetHub<THub>(this IGrainFactory grainFactory)
        {
            return new HubContext<THub>(grainFactory);
        }

    }
}
