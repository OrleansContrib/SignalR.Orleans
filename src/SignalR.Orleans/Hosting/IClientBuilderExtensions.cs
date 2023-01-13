using Microsoft.Extensions.DependencyInjection;
using SignalR.Orleans;
using SignalR.Orleans.Clients;

// ReSharper disable once CheckNamespace
namespace Orleans.Hosting
{
    public static class IClientBuilderExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder, Action<SignalRClientConfig>? configure = null)
        {
            var cfg = new SignalRClientConfig();
            configure?.Invoke(cfg);
            return builder.UseSignalR(cfg);
        }

        public static IClientBuilder UseSignalR(this IClientBuilder builder, SignalRClientConfig? config = null)
        {
            config ??= new SignalRClientConfig();
            return builder.AddMemoryStreams(Constants.STREAM_PROVIDER);
        }
    }
}