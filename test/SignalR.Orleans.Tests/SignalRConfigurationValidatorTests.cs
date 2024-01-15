using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using System.Net;
using Xunit;

namespace SignalR.Orleans.Tests;

public sealed class SignalRConfigurationValidatorTests
{
	// [Fact]
	// public void ValidateConfiguration_Throws_IfNoPubSubProviderIsRegistered()
	// {
	// 	var siloHost = new HostBuilder()
	// 		.UseOrleans(
	// 			builder => builder
	// 				.UseLocalhostClustering()
	// 				.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
	// 				.UseSignalR()
	// 		)
	// 		.Build();

	// 	Assert.Throws<InvalidOperationException>(() => siloHost.Start());
	// 	siloHost.Dispose();
	// }

	// [Fact]
	// public void ValidateConfiguration_DoesNotThrow_IfPubSubProviderIsRegistered()
	// {
	// 	var siloHost = new HostBuilder()
	// 		.UseOrleans(
	// 			builder => builder
	// 				.UseLocalhostClustering()
	// 				.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
	// 				.AddMemoryGrainStorage(Constants.PUBSUB_PROVIDER)
	// 				.UseSignalR()
	// 		)
	// 		.Build();

	// 	siloHost.Start();
	// 	siloHost.Dispose();
	// }

}