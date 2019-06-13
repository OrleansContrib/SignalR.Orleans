using System;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
{
    public static class SiloBuilderExtensions
    {
        public static ISiloBuilder UseSignalR(this ISiloBuilder builder, Action<SignalrOrleansSiloConfigBuilder> configure = null)
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

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = cfg.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }

    public static class SiloHostBuilderExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder, Action<SignalrOrleansSiloHostConfigBuilder> configure = null)
        {
            var cfg = new SignalrOrleansSiloHostConfigBuilder();
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

            return builder
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = cfg.UseFireAndForgetDelivery)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
        }
    }
}