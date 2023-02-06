using Orleans.Hosting;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OrleansClientExtensions
{
	public static IClientBuilder UseSignalR(this IClientBuilder builder, Action<SignalrClientConfig> config)
	{
		var cfg = new SignalrClientConfig();
		config?.Invoke(cfg);

		return builder.UseSignalR(cfg);
	}

	public static IClientBuilder UseSignalR(this IClientBuilder builder, SignalrClientConfig config = null)
	{
		if (config == null)
			config = new SignalrClientConfig();

		return builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = config.UseFireAndForgetDelivery)
			.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences());
	}
}