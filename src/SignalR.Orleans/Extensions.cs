using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime.Configuration;
using Orleans.Serialization;
using SignalR.Orleans;
using SignalR.Orleans.Clients;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrleansDependencyInjectionExtensions
    {
        public static ClusterConfiguration AddSignalR(this ClusterConfiguration config)
        {
            // TODO: Check with @galvesribeiro if its needed, to be fixed or deleted.
            // config.Globals.SerializationProviders.Add(typeof(HubMessageSerializer).GetTypeInfo());
            config.Globals.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();
            config.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER);
            try
            {
                config.AddMemoryStorageProvider("PubSubStore");
            }
            catch
            {
                // PubSubStore was already added. Do nothing.
            }
            config.AddMemoryStorageProvider(Constants.STORAGE_PROVIDER);
            return config;
        }

        public static ClientConfiguration AddSignalR(this ClientConfiguration config)
        {
            // TODO: Check with @galvesribeiro if its needed, to be fixed or deleted.
            // config.SerializationProviders.Add(typeof(HubMessageSerializer).GetTypeInfo());
            config.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();
            config.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER);
            return config;
        }

        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder)
        {
            return builder
                .AddApplicationPartsFromReferences(typeof(ClientGrain).Assembly);
        }

        public static IClientBuilder UseSignalR(this IClientBuilder builder)
        {
            return builder
                .AddApplicationPartsFromReferences(typeof(IClientGrain).Assembly);
        }

        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder, IClientBuilder clientBuilder)
        {
            var client = clientBuilder.Build();
            client.Connect().Wait();
            return builder.AddOrleans(client);
        }

        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder, IClusterClient clusterClient)
        {
            builder.Services.AddSingleton(typeof(IClusterClient), clusterClient);
            return builder.AddOrleans();
        }

        public static ISignalRBuilder AddOrleans(this ISignalRBuilder builder)
        {
            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>));
            return builder;
        }
    }
}
