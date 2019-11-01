using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.Orleans;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder, IClusterClientProvider clientProvider = null)
        {
            if (clientProvider != null)
                builder.Services.AddSingleton(clientProvider);
            else
                builder.Services.TryAddSingleton<IClusterClientProvider, DefaultClusterClientProvider>();

            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
            return builder;
        }
    }
}
