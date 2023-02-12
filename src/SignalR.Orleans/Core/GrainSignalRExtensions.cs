using Microsoft.AspNetCore.SignalR.Protocol;
using SignalR.Orleans.Core;

// ReSharper disable once CheckNamespace
namespace Orleans;

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
        var invocationMessage = new InvocationMessage(methodName, args);
        return grain.Send(invocationMessage);
    }

    /// <summary>
    /// Invokes a method on the hub and ignores the Task.
    /// Note: This is a fire-and-forget method. The Task will not be awaited and any exception will be swallowed.
    /// </summary>
    /// <param name="grain"></param>
    /// <param name="methodName">Target method name to invoke.</param>
    /// <param name="args">Arguments to pass to the target method.</param>
    /// <returns></returns>
    public static Task SendOneWay(this IHubMessageInvoker grain, string methodName, params object?[] args)
    {
        grain.SendOneWay(methodName, args).Ignore();

        return Task.CompletedTask;
    }
}
