using System;
using Orleans.Hosting;

namespace SignalR.Orleans
{
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

    [Obsolete("Use SignalrOrleansSiloHostConfigBuilder instead.")]
    public class SignalrServerConfig
    {
        public Action<ISiloHostBuilder, HostBuilderConfig> ConfigureBuilder { get; set; } = default!;
        public bool UseFireAndForgetDelivery { get; set; }
    }

    public class SignalrOrleansConfigBaseBuilder
    {
        public bool UseFireAndForgetDelivery { get; set; }
    }

    public class SignalrOrleansSiloConfigBuilder : SignalrOrleansConfigBaseBuilder
    {
        internal Action<ISiloBuilder, HostBuilderConfig> ConfigureBuilder { get; set; } = default!;

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

    public class SignalrOrleansSiloHostConfigBuilder : SignalrOrleansConfigBaseBuilder
    {
        internal Action<ISiloHostBuilder, HostBuilderConfig> ConfigureBuilder { get; set; } = default!;

        /// <summary>
        /// Configure builder, such as providers.
        /// </summary>
        /// <param name="configure">Configure action. This may be called multiple times.</param>
        public SignalrOrleansSiloHostConfigBuilder Configure(Action<ISiloHostBuilder, HostBuilderConfig> configure)
        {
            ConfigureBuilder += configure;
            return this;
        }
    }

    public class SignalrClientConfig
    {
        public bool UseFireAndForgetDelivery { get; set; }
    }
}
