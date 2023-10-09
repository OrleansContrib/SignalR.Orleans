using Microsoft.Extensions.DependencyInjection;

namespace SignalR.Orleans;

public class HostBuilderConfig
{
	/// <summary>
	/// Gets the storage provider name which is used for registration.
	/// </summary>
	public string StorageProvider { get; } = Constants.STORAGE_PROVIDER;

	/// <summary>
	/// Gets the pubsub provider name which is used for registration.
	/// </summary>
	public string PubSubProvider { get; } = Constants.PUBSUB_PROVIDER;
}

public class SignalrOrleansConfigBaseBuilder : ISiloMemoryStreamConfigurator
{
	public bool UseFireAndForgetDelivery { get; set; }
	public string Name { get; }
	public Action<Action<IServiceCollection>> ConfigureDelegate { get; }
}

public class SignalrOrleansSiloConfigBuilder : SignalrOrleansConfigBaseBuilder
{
	internal Action<ISiloBuilder, HostBuilderConfig> ConfigureBuilder { get; set; }

	/// <summary>
	/// Configure builder, such as providers.
	/// </summary>
	/// <param name="configure">Configure action. This may be called multiple times.</param>
	public SignalrOrleansSiloConfigBuilder Configure(Action<ISiloBuilder, HostBuilderConfig> configure)
	{
		ConfigureBuilder += configure;
		return this;
	}
}

public class SignalrClientConfig
{
	public bool UseFireAndForgetDelivery { get; set; }
}