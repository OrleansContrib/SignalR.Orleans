using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Storage;
using SignalR.Orleans;
using SignalR.Orleans.Clients;
using System;
using Microsoft.AspNetCore.SignalR;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
{
    public static class SiloBuilderExtensions
    {
        public static ISiloBuilder UseSignalR(this ISiloBuilder builder,
            Action<SignalrOrleansSiloConfigBuilder> configure = null)
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
                    sp.GetRequiredService<HubLifetimeManager<THub>>() as ILifecycleParticipant<ISiloLifecycle>);
            });

            return builder;
        }
    }

    public static class SiloHostBuilderExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder,
            Action<SignalrOrleansSiloHostConfigBuilder> configure = null)
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