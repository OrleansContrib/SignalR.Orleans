using Microsoft.Extensions.Hosting;

namespace SignalR.Orleans
{
    public class SignalROrleansHostConfigBuilder : SignalROrleansConfigBaseBuilder
    {
        internal Action<IHostBuilder> ConfigureBuilder { get; set; } = default!;

        /// <summary>
        /// Configure builder, such as providers.
        /// </summary>
        /// <param name="configure">Configure action. This may be called multiple times.</param>
        public SignalROrleansHostConfigBuilder Configure(Action<IHostBuilder> configure)
        {
            ConfigureBuilder += configure;
            return this;
        }
    }
}
