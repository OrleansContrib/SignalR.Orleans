using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Hosting;
using SignalR.Orleans;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrleansServerExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder)
        {
            try { builder = builder.AddMemoryGrainStorage("PubSubStore"); }
            catch { /** PubSubStore was already added. Do nothing. **/ }

            return builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER)
                .AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER)
                .ConfigureApplicationParts(parts => parts.AddFromAppDomain().AddFromApplicationBaseDirectory());
        }
    }

    public static class OrleansClientExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder)
        {
            return builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER)
                .ConfigureApplicationParts(parts => parts.AddFromAppDomain().AddFromApplicationBaseDirectory())
                .ConfigureServices(services => services.AddSingleton(typeof(HubLifetimeManager<>), typeof(OrleansHubLifetimeManager<>)));
        }
    }
}
