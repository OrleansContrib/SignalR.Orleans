using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Microsoft.Extensions.Hosting;

namespace SignalR.Orleans.Tests
{
    public class OrleansFixture : IDisposable
    {
        public IHost Silo { get; }
        public IClusterClient Client { get; }
        public IHost ClientHost { get; }

        public OrleansFixture()
        {
            Silo = new HostBuilder()
                .UseOrleans(siloBuilder =>
                {
                    siloBuilder.UseLocalhostClustering();
                    siloBuilder.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback);
                    siloBuilder.UseSignalR();
                })
                .Build();

            Silo.StartAsync().GetAwaiter().GetResult();

            ClientHost = new HostBuilder()
                .UseOrleansClient(clientBuilder =>
                {
                    clientBuilder.UseLocalhostClustering();
                    clientBuilder.UseSignalR(config: null); // fixes compiler confusion
                })
                .Build();

            ClientHost.StartAsync().GetAwaiter().GetResult();
            Client = ClientHost.Services.GetRequiredService<IClusterClient>();
        }

        public void Dispose()
        {
            ClientHost.StopAsync().GetAwaiter().GetResult();
            Silo.StopAsync().GetAwaiter().GetResult();
            ClientHost.Dispose();
            Silo.Dispose();
        }
    }
}
