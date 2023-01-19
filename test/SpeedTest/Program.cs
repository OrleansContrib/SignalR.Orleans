using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using SignalR.Orleans;
using SignalR.Orleans.Tests.AspnetSignalR;
using SignalR.Orleans.Tests.Models;

var loggerFactory = new LoggerFactory();
// Creates 10 silos
var silos = await CreateSilos(10);
// Creates a signalR hub for each silo
var managers = silos.Select(silo => new OrleansHubLifetimeManager<DaHub>(loggerFactory.CreateLogger<OrleansHubLifetimeManager<DaHub>>(), silo.Client)).ToArray();
// Connects 1000 clients to each signalR Hub (total 10,000 clients)
var signalRClients = await CreateSignalRClients();

await Task.WhenAll(SendAllMessages(), ReadAllMessages());

var sw = Stopwatch.StartNew();
for (var i = 0; i < 10; i++)
{
    // Send 100 messages to 10,000 clients for a total of 1 million messages.
    await Task.WhenAll(SendAllMessages(), ReadAllMessages());
}
var elapsed = sw.Elapsed; // 4.3 seconds
Debugger.Break();

async Task SendAllMessages()
{
    var sendTasks = managers.Select(async manager =>
    {
        for (var i = 0; i < 10; i++)
        {
            await manager.SendAllAsync("Test", new object?[] { "Test" });
        }
    }).ToArray();
    await Task.WhenAll(sendTasks);
}

async Task ReadAllMessages()
{
    for (var i = 0; i < 100; i++)
    {
        foreach (var clientArray in signalRClients)
        {
            foreach (var client in clientArray)
            {
                var msg = await client.ReadAsync();
            }
        }
    }
}

async Task<TestClient[][]> CreateSignalRClients()
{
    var creationTasks = managers.Select(async manager =>
    {
        var testClients = new TestClient[1000];
        for (var i = 0; i < 1000; i++)
        {
            var testClient = new TestClient();
            var connection1 = HubConnectionContextUtils.Create(testClient.Connection);
            await manager.OnConnectedAsync(connection1).OrTimeout();
            testClients[i] = testClient;
        }
        return testClients;
    }).ToArray();
    await Task.WhenAll(creationTasks);
    return creationTasks.Select(t => t.Result).ToArray();
}

async Task<IReadOnlyList<(IHost Host, IClusterClient Client)>> CreateSilos(int numSilos)
{
    var silos = new List<(IHost Host, IClusterClient Client)>();
    var primarySilo = await CreateSilo(null);
    silos.Add(primarySilo);
    var primarySiloEndpoint = primarySilo.Host.Services.GetRequiredService<ILocalSiloDetails>().SiloAddress.Endpoint;
    var siloTasks = Enumerable.Range(0, numSilos - 1).Select(i => CreateSilo(primarySiloEndpoint)).ToArray();
    await Task.WhenAll(siloTasks);
    silos.AddRange(siloTasks.Select(t => t.Result));
    return silos;
}

async Task<(IHost Host, IClusterClient Client)> CreateSilo(IPEndPoint? primarySiloEndpoint)
{
    var host = new HostBuilder()
      .UseOrleans(siloBuilder =>
      {
          siloBuilder.UseDevelopmentClustering(primarySiloEndpoint);
          siloBuilder.UseSignalR();
          //siloBuilder.ConfigureLogging(logging =>
          //{
          //    logging.AddConsole();
          //    logging.AddFilter(level => level >= LogLevel.Warning);
          //});

      })
      .UseConsoleLifetime()
      .Build();
    await host.StartAsync();

    var gatewayPort = host.Services.GetRequiredService<ILocalSiloDetails>().GatewayAddress.Endpoint;
    var clientHost = new HostBuilder()
      .UseOrleansClient(clientBuilder =>
      {
          clientBuilder.UseStaticClustering(gatewayPort);
          clientBuilder.UseSignalR(config: null);
      })
      .Build();

    await clientHost.StartAsync();
    var client = clientHost.Services.GetRequiredService<IClusterClient>();

    return (host, client);
}

