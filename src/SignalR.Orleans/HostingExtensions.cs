using System;
using Orleans.Runtime;
using Orleans.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
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
        public static IClientBuilder UseSignalR(this IClientBuilder builder, Action<SignalrClientConfig> config)
        {
            var cfg = new SignalrClientConfig();
            config?.Invoke(cfg);

            return builder.UseSignalR(cfg);
        }

        public static IClientBuilder UseSignalR(this IClientBuilder builder, SignalrClientConfig? config = null)
        {
            if (config == null)
                config = new SignalrClientConfig();

            return builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = config.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences());
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder, IClusterClientProvider? clientProvider = null)
        {
            if (clientProvider != null)
                builder.Services.AddSingleton(clientProvider);
            else
                builder.Services.TryAddSingleton<IClusterClientProvider, DefaultClusterClientProvider>();

            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
            return builder;
        }
    }

    public static class SiloBuilderExtensions
    {
        public static ISiloBuilder UseSignalR(this ISiloBuilder builder,
            Action<SignalrOrleansSiloConfigBuilder>? configure = null)
        {
            var cfg = new SignalrOrleansSiloConfigBuilder();
            configure?.Invoke(cfg);

            cfg.ConfigureBuilder?.Invoke(builder, new HostBuilderConfig());

            try
            {
                builder.AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER);
            }
            catch
            {
                /** PubSubStore was already added. Do nothing. **/
            }

            try
            {
                builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER);
            }
            catch
            {
                /** Grain storage provider was already added. Do nothing. **/
            }

            builder.ConfigureServices(services =>
                services.AddSingleton<IConfigurationValidator, SignalRConfigurationValidator>());

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER,
                    opt => opt.FireAndForgetDelivery = cfg.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }

        public static ISiloBuilder RegisterHub<THub>(this ISiloBuilder builder) where THub : Hub
        {
            builder.ConfigureServices(services =>
            {
                services.AddTransient<ILifecycleParticipant<ISiloLifecycle>>(sp =>
                    (sp.GetRequiredService<HubLifetimeManager<THub>>() as ILifecycleParticipant<ISiloLifecycle>)!);
            });

            return builder;
        }
    }

    public static class SiloHostBuilderExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder,
            Action<SignalrOrleansSiloHostConfigBuilder>? configure = null)
        {
            var cfg = new SignalrOrleansSiloHostConfigBuilder();
            configure?.Invoke(cfg);

            cfg.ConfigureBuilder?.Invoke(builder, new HostBuilderConfig());

            try
            {
                builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER);
            }
            catch
            {
                /** Grain storage provider was already added. Do nothing. **/
            }

            builder.ConfigureServices(services =>
                services.AddSingleton<IConfigurationValidator, SignalRConfigurationValidator>());

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER,
                    opt => opt.FireAndForgetDelivery = cfg.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }

    internal class SignalRConfigurationValidator : IConfigurationValidator
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _sp;

        public SignalRConfigurationValidator(IServiceProvider serviceProvider)
        {
            this._logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<SignalRConfigurationValidator>();
            this._sp = serviceProvider;
        }

        public void ValidateConfiguration()
        {
            this._logger.LogInformation("Checking if a PubSub storage provider was registered...");

            var pubSubProvider = this._sp.GetServiceByName<IGrainStorage>(Constants.PUBSUB_PROVIDER);
            if (pubSubProvider == null)
            {
                var err =
                    "No PubSub storage provider was registered. You need to register one. To use the default/in-memory provider, call 'siloBuilder.AddMemoryGrainStorage(\"PubSubStore\")' when building your Silo.";
                this._logger.LogError(err);
                throw new InvalidOperationException(err);
            }

            this._logger.LogInformation(
                $"Found the PubSub storage provider of type '{pubSubProvider.GetType().FullName}'.");
        }
    }
}