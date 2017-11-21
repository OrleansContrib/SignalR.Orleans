using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Core;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Users;
using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Orleans
{
    public static class GrainSignalRExtensions
    {
        public static async Task SendSignalRMessage(this IConnectionGrain grain, string methodName, params object[] message)
        {
            var invocationMessage = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: message);
            await grain.SendMessage(invocationMessage);
        }
    }

    public static class GrainFactoryExtensions
    {
        public static HubContext<THub> GetHub<THub>(this IGrainFactory grainFactory)
        {
            return new HubContext<THub>(grainFactory);
        }

        internal static IClientGrain GetClientGrain(this IGrainFactory factory, string hubName, string connectionId)
            => factory.GetGrain<IClientGrain>(Utils.BuildGrainName(hubName, connectionId));

        internal static IGroupGrain GetGroupGrain(this IGrainFactory factory, string hubName, string groupName)
            => factory.GetGrain<IGroupGrain>(Utils.BuildGrainName(hubName, groupName));

        internal static IUserGrain GetUserGrain(this IGrainFactory factory, string hubName, string userId)
            => factory.GetGrain<IUserGrain>(Utils.BuildGrainName(hubName, userId));
    }
}
