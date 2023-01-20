using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;
using Orleans.Streams;
using SignalR.Orleans.Core;

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

        // TODO: Implement this
        // public static Task SendOneWay(this IHubMessageInvoker grain, string methodName, params object?[] args)
        // {
        //     var invocationMessage = new InvocationMessage(methodName, args).AsImmutable();
        //     return grain.SendOneWay(invocationMessage);
        // }
    }
}