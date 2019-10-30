using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using Orleans.Hosting;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrleansServerExtensions
    {
#pragma warning disable 618
        [Obsolete("Use UseSignalR(this ISiloBuilder builder, Action<SignalrOrleansSiloConfigBuilder> configure = null) from Orleans.Hosting instead.")]
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder, Action<SignalrServerConfig> config)
        {
            var cfg = new SignalrServerConfig();
            config?.Invoke(cfg);

            return builder.UseSignalR(cfg);
        }

        [Obsolete("Use UseSignalR(this ISiloBuilder builder, Action<SignalrOrleansSiloConfigBuilder> configure = null) from Orleans.Hosting instead.")]
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder, SignalrServerConfig config)
        {
            if (config == null)
                config = new SignalrServerConfig();
#pragma warning restore 618

            config.ConfigureBuilder?.Invoke(builder, new HostBuilderConfig());

            try { builder.AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER); }
            catch { /* PubSubStore was already added. Do nothing. */ }

            try { builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER); }
            catch { /* Grain storage provider was already added. Do nothing. */ }

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = config.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }

    public static class OrleansClientExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder, Action<SignalrClientConfig> config)
        {
            var cfg = new SignalrClientConfig();
            config?.Invoke(cfg);

            return builder.UseSignalR(cfg);
        }

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
