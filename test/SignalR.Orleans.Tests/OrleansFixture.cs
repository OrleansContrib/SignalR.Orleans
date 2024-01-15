using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using System.Net;

namespace SignalR.Orleans.Tests;

public class OrleansFixture : IDisposable
{
	public IHost Silo { get; }
	public IClusterClient Client { get; }
	public IHost ClientHost { get; }

	public OrleansFixture()
	{
		var siloHost = new HostBuilder()
			.UseOrleans(builder => builder
				.UseLocalhostClustering()
				.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
				.AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER)
				.UseSignalR()
			)
			.Build();
		siloHost.StartAsync().Wait();
		Silo = siloHost;

		ClientHost = new HostBuilder()
			.UseOrleansClient(client => client
				.UseLocalhostClustering()
				.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
				.UseSignalR()
			)
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