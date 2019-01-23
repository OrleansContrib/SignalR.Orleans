using System;
using Orleans.Hosting;

namespace SignalR.Orleans
{
    public class HostBuilderConfig
    {
        public string StorageProvider { get; } = Constants.STORAGE_PROVIDER;
        public string PubSubProvider { get; } = Constants.PUBSUB_PROVIDER;
    }

    public class SignalrServerConfig
    {
        public Action<ISiloHostBuilder, HostBuilderConfig> ConfigureProviders { get; set; }
        public bool UseFireAndForgetDelivery { get; set; }
    }

    public class SignalrClientConfig
    {
        public bool UseFireAndForgetDelivery { get; set; }
    }
}
