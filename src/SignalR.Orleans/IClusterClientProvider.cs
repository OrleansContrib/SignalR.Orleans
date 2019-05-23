using Orleans;

namespace SignalR.Orleans
{
    public interface IClusterClientProvider
    {
        IClusterClient GetClient();
    }

    internal class DefaultClusterClientProvider : IClusterClientProvider
    {
        private readonly IClusterClient _clusterClient;

        public DefaultClusterClientProvider(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public IClusterClient GetClient() => _clusterClient;
    }
}
