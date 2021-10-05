using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;

namespace SignalR.Orleans.Tests
{
    public class OrleansFixture : IDisposable
    {
        public ISiloHost Silo { get; }
        public IClusterClientProvider ClientProvider { get; }

        public OrleansFixture()
        {
            var silo = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER)
                .UseSignalR()
                .Build();
            silo.StartAsync().Wait();
            Silo = silo;

            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .UseSignalR()
                .Build();

            client.Connect().Wait();
            ClientProvider = new DefaultClusterClientProvider(client);
        }

        public void Dispose()
        {
            ClientProvider.GetClient().Close().Wait();
            Silo.StopAsync().Wait();
        }
    }
}