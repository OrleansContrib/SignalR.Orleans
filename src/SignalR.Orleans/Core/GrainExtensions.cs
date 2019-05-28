using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Core;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Users;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class GrainSignalRExtensions
    {
        [Obsolete("Use Send instead", false)]
        public static async Task SendSignalRMessage(this IConnectionGrain grain, string methodName, params object[] message)
        {
            var invocationMessage = new InvocationMessage(methodName, message);
            await grain.Send(invocationMessage);
        }

        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        public static Task Send(this IHubMessageInvoker grain, string methodName, params object[] args)
        {
            var invocationMessage = new InvocationMessage(methodName, args);
            return grain.Send(invocationMessage);
        }
    }

    public static class GrainFactoryExtensions
    {
        public static HubContext<THub> GetHub<THub>(this IGrainFactory grainFactory)
        {
            return new HubContext<THub>(grainFactory);
        }

        internal static IClientGrain GetClientGrain(this IGrainFactory factory, string hubName, string connectionId)
            => factory.GetGrain<IClientGrain>(ConnectionGrainKey.Build(hubName, connectionId));

        internal static IGroupGrain GetGroupGrain(this IGrainFactory factory, string hubName, string groupName)
            => factory.GetGrain<IGroupGrain>(ConnectionGrainKey.Build(hubName, groupName));

        internal static IUserGrain GetUserGrain(this IGrainFactory factory, string hubName, string userId)
            => factory.GetGrain<IUserGrain>(ConnectionGrainKey.Build(hubName, userId));
    }
}