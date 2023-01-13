using Orleans.Runtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
{
    public static class ISiloBuilderExtensions
    {
        public static ISiloBuilder UseSignalR(this ISiloBuilder builder, Action<SignalROrleansSiloConfigBuilder>? configure = null)
        {
            var cfg = new SignalROrleansSiloConfigBuilder();
            configure?.Invoke(cfg);

            cfg.ConfigureBuilder?.Invoke(builder);

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

            builder.AddMemoryStreams(Constants.STREAM_PROVIDER);

            return builder;
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
}