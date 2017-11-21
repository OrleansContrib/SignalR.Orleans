using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime.Configuration;
using Orleans.Serialization;
using System;
using System.Reflection;

namespace SignalR.Orleans.Tests
{
    public class OrleansFixture : IDisposable
    {
        public ISiloHost Silo { get; }
        public IClusterClient Client { get; }

        public OrleansFixture()
        {
            var siloConfig = ClusterConfiguration.LocalhostPrimarySilo()
                .AddSignalR();
            siloConfig.Globals.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();
            var silo = new SiloHostBuilder()
                .UseConfiguration(siloConfig)
                .UseSignalR()
                .Build();
            silo.StartAsync().Wait();
            this.Silo = silo;

            var clientConfig = ClientConfiguration.LocalhostSilo()
                .AddSignalR();

            clientConfig.FallbackSerializationProvider = typeof(ILBasedSerializer).GetTypeInfo();

            var client = new ClientBuilder().UseConfiguration(clientConfig)
                .UseSignalR()
                .Build();

            client.Connect().Wait();
            this.Client = client;
        }
        public void Dispose()
        {
            Client.Close().Wait();
            Silo.StopAsync().Wait();
        }
    }
}