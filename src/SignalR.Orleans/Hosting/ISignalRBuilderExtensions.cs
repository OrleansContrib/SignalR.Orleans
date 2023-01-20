using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.Orleans;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
{
    public static class ISignalRBuilderExtensions
    {
        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder)
        {
            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
            return builder;
        }

        public static ISignalRServerBuilder AddOrleans(this ISignalRServerBuilder builder)
        {
            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
            return builder;
        }
    }
}