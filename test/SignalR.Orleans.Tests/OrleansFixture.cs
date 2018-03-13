using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;

namespace SignalR.Orleans.Tests
{
    public class OrleansFixture : IDisposable
    {
        public ISiloHost Silo { get; }
        public IClusterClient Client { get; }

        public OrleansFixture()
        {
            var siloPort = 11111;
            int gatewayPort = 30000;
            var siloAddress = IPAddress.Loopback;

            var silo = new SiloHostBuilder()
                .Configure<ClusterOptions>(options => options.ClusterId = "test-cluster")
                .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
                .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
                .UseSignalR()
                .Build();
            silo.StartAsync().Wait();
            this.Silo = silo;

            var client = new ClientBuilder()
                .Configure<ClusterOptions>(options => options.ClusterId = "test-cluster")
                .UseStaticClustering(options => options.Gateways.Add(new IPEndPoint(siloAddress, gatewayPort).ToGatewayUri()))
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