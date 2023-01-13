using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using Microsoft.Extensions.Hosting;

namespace SignalR.Orleans.Tests
{
    public class OrleansFixture : IDisposable
    {
        public IHost Silo { get; }
        public IClusterClient Client { get; }

        public OrleansFixture()
        {
            var silo = new HostBuilder()
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder.UseLocalhostClustering();
                    siloBuilder.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback);
                    siloBuilder.UseSignalR();
                })
                .Build();

            silo.StartAsync().GetAwaiter().GetResult();
            Silo = silo;

            new client

            var client = new ClientBuilder(silo.Services)
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