using Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;

namespace Silo
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var host = new HostBuilder()
                    .UseOrleans((context, siloBuilder) =>
                    {
                        siloBuilder
                            .UseSignalR(builder =>
                            {
                                builder
                                    .Configure((innerSiloBuilder, config) =>
                                    {
                                        innerSiloBuilder
                                            .UseLocalhostClustering(serviceId: "HelloWorldApp", clusterId: "dev")
                                            .AddMemoryGrainStorage("PubSubStore")
                                            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(UserNotificationGrain).Assembly).WithReferences());
                                    });
                            });
                    })
                    .ConfigureLogging((context, logging) =>
                    {
                        logging.AddConsole();
                    })
                    .Build();
            await host.RunAsync();

            return 0;
        }
    }
}
