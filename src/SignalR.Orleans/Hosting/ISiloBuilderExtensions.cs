using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using SignalR.Orleans;
using SignalR.Orleans.Core;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting;

public static class ISiloBuilderExtensions
{
    public static ISiloBuilder UseSignalR(this ISiloBuilder builder, Action<SignalROrleansSiloConfigBuilder>? configure = null)
    {
        var cfg = new SignalROrleansSiloConfigBuilder();
        configure?.Invoke(cfg);

        cfg.ConfigureBuilder?.Invoke(builder);

        try
        {
            //builder.AddMemoryGrainStorage(SignalROrleansConstants.PUBSUB_STORAGE_PROVIDER); // "ORLEANS_SIGNALR_PUBSUB_PROVIDER"
        }
        catch
        {
            /** PubSubStore was already added. Do nothing. **/
        }

        try
        {
            //builder.AddMemoryGrainStorage(SignalROrleansConstants.SIGNALR_ORLEANS_STORAGE_PROVIDER); // "ORLEANS_SIGNALR_STORAGE_PROVIDER"
        }
        catch
        {
            /** Grain storage provider was already added. Do nothing. **/
        }

        builder.ConfigureServices(services =>
        {
            services.Configure<InternalOptions>(options =>
                {
                    options.ConflateStorageAccess = cfg.ConflateStorageAccess;
                });
        });

        //builder.AddMemoryStreams(SignalROrleansConstants.SIGNALR_ORLEANS_STREAM_PROVIDER); // "ORLEANS_SIGNALR_STREAM_PROVIDER"

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