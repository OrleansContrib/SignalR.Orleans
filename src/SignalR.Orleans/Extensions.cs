using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using Orleans.Hosting;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrleansServerExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder, bool useFireAndForgetDelivery = false)
        {
            try { builder = builder.AddMemoryGrainStorage("PubSubStore"); }
            catch { /** PubSubStore was already added. Do nothing. **/ }

            return builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER)
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = useFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }

    public static class OrleansClientExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder, bool useFireAndForgetDelivery = false)
        {
            return builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = useFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences());
        }
    }

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
