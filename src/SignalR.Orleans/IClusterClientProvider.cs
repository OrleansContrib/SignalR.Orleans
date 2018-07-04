using Orleans;

namespace SignalR.Orleans
{
    public interface IClusterClientProvider
    {
        IClusterClient GetClient();
    }

    internal class DefaultClusterClientProvider : IClusterClientProvider
    {
        private IClusterClient _clusterClient;

        public DefaultClusterClientProvider(IClusterClient clusterClient)
        {
            this._clusterClient = clusterClient;
        }

        public IClusterClient GetClient() => this._clusterClient;
    }
}
