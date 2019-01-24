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
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder, SignalrServerConfig config = null)
        {
            if (config == null)
                config = new SignalrServerConfig();

            config.ConfigureBuilder?.Invoke(builder, new HostBuilderConfig());

            try { builder.AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER); }
            catch { /** PubSubStore was already added. Do nothing. **/ }

            try { builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER); }
            catch { /** Grain storage provider was already added. Do nothing. **/ }

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = config.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }

    public static class OrleansClientExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder, SignalrClientConfig config = null)
        {
            if (config == null)
                config = new SignalrClientConfig();

            return builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = config.UseFireAndForgetDelivery)
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
