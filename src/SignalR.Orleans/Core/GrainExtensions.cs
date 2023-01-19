using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;
using SignalR.Orleans.Core;
using SignalR.Orleans.Users;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class GrainSignalRExtensions
    {
        /// <summary>
        /// Invokes a method on the hub.
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        public static Task Send(this IHubMessageInvoker grain, string methodName, params object?[] args)
        {
            var invocationMessage = new InvocationMessage(methodName, args).AsImmutable();
            return grain.Send(invocationMessage);
        }

        /// <summary>
        /// Invokes a method on the hub (one way).
        /// </summary>
        /// <param name="grain"></param>
        /// <param name="methodName">Target method name to invoke.</param>
        /// <param name="args">Arguments to pass to the target method.</param>
        public static void SendOneWay(this IHubMessageInvoker grain, string methodName, params object?[] args)
        {
            grain.InvokeOneWay(g => g.Send(methodName, args));
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

        internal static IServerDirectoryGrain GetServerDirectoryGrain(this IGrainFactory factory)
            => factory.GetGrain<IServerDirectoryGrain>(0);
    }
}