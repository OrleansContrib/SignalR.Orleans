using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting;

public static class SiloBuilderExtensions
{
	public static ISiloBuilder UseSignalR(this ISiloBuilder builder, Action<SignalrOrleansSiloConfigBuilder> configure = null)
	{
		var cfg = new SignalrOrleansSiloConfigBuilder();
		configure?.Invoke(cfg);

		cfg.ConfigureBuilder?.Invoke(builder, new HostBuilderConfig());

		try { builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER); }
		catch { /* Grain storage provider was already added. Do nothing. */ }

		builder.ConfigureServices(services => services.AddSingleton<IConfigurationValidator, SignalRConfigurationValidator>());

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

		try { builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER); }
		catch { /* Grain storage provider was already added. Do nothing. */ }

		builder.ConfigureServices(services => services.AddSingleton<IConfigurationValidator, SignalRConfigurationValidator>());

		return builder
			.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, opt => opt.FireAndForgetDelivery = cfg.UseFireAndForgetDelivery)
			.ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences());
	}
}

internal class SignalRConfigurationValidator : IConfigurationValidator
{
	private readonly IServiceProvider _sp;
	private readonly ILogger _logger;

	public SignalRConfigurationValidator(IServiceProvider serviceProvider)
	{
		_logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SignalRConfigurationValidator>();
		_sp = serviceProvider;
	}

	public void ValidateConfiguration()
	{
		_logger.LogInformation("Checking if a PubSub storage provider was registered...");

		var pubSubProvider = _sp.GetServiceByName<IGrainStorage>(Constants.PUBSUB_PROVIDER);
		if (pubSubProvider == null)
		{
			var err = "No PubSub storage provider was registered. You need to register one. To use the default/in-memory provider, call 'siloBuilder.AddMemoryGrainStorage(\"PubSubStore\")' when building your Silo.";
			_logger.LogError(err);
			throw new InvalidOperationException(err);
		}

		_logger.LogInformation($"Found the PubSub storage provider of type '{pubSubProvider.GetType().FullName}'.");
	}
}