<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="SignalR.Orleans" width="300px"> 
  <h1>SignalR.Orleans</h1>
</p>

[![Build](https://github.com/OrleansContrib/SignalR.Orleans/workflows/CI/badge.svg)](https://github.com/OrleansContrib/SignalR.Orleans/actions)
[![Package Version](https://img.shields.io/nuget/v/SignalR.Orleans.svg)](https://www.nuget.org/packages/SignalR.Orleans)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SignalR.Orleans.svg)](https://www.nuget.org/packages/SignalR.Orleans)
[![License](https://img.shields.io/github/license/OrleansContrib/SignalR.Orleans.svg)](https://github.com/OrleansContrib/SignalR.Orleans/blob/master/LICENSE)
[![Discord](https://img.shields.io/discord/333727978460676096)](https://discord.gg/TCSAgM9k)

[Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/overview) is a cross-platform framework for building robust, scalable distributed applications. Distributed applications are defined as apps that span more than a single process, often beyond hardware boundaries using peer-to-peer communication. Orleans scales from a single on-premises server to hundreds to thousands of distributed, highly available applications in the cloud. [See Orleans source code on Github](https://github.com/dotnet/orleans)

[ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-7.0) is an open-source library that simplifies adding real-time web functionality to apps. Real-time web functionality enables server-side code to push content to clients instantly.

**SignalR.Orleans** is a package that gives you two abilities: 

1. Use your Orleans cluster as a backplane for SignalR. [Learn about scaling out SignalR on multiple servers.](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-7.0)

> There are various choices of backplane that you can use for SignalR, as you will see in the link above. If you're already using Orleans, then you might want to use Orleans as the backplane to reduce the number of dependencies used by your application and to reduce the number of network hops (and latency) that would be required when calling an external service.

2. Send messages from Orleans grains to SignalR clients.

> If the SignalR component of your application is cohosted with Orleans (same server, same process and `Microsoft.AspNetCore.SignalR.IHubContext<MyHub>` can be injected into an Orleans gain), you already have this ability without installing this package.

> However, if the SignalR component of your application is "remote" from the grains, this package will give the grains a way of sending messages to SignalR clients by injecting `SignalR.Orleans.Core.HubContext<MyHub>`.

TODO: These two abilities should be provided independently of each other. Unfortunately at this stage, ability #2 is only provided if ability #1 is used as well.

# Installation

Installation is performed via [NuGet.](https://www.nuget.org/packages/SignalR.Orleans/)

Packages with version `7.x.x` are compatible with Orleans `v7.x.x` and above. If you're still using an earlier version of Orleans, you will need to use earlier versions of the package.

Package Manager:

> PS> Install-Package SignalR.Orleans

.Net CLI:

> \# dotnet add package SignalR.Orleans

Paket:

> \# paket add SignalR.Orleans

---
# Version 7.0.0 documentation
> Scroll down to see documentation for earlier versions.

Here is a complete starter example featuring cohosted aspnetcore app with SignalR and Orleans.

```csharp
using Microsoft.AspNetCore.SignalR;
using Orleans.Hosting;
using SignalR.Orleans;

// Create a host that can cohost aspnetcore AND orleans together in a single process.
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseOrleans(siloBuilder => 
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.UseSignalR(); // Adds ability #1 and #2 to Orleans.
    siloBuilder.RegisterHub<MyHub>(); // Required for each hub type if the backplane ability #1 is being used.
});

builder.Services
    .AddSignalR()  // Adds SignalR hubs to the web application
    .AddOrleans(); // Tells the SignalR hubs in the web application to use Orleans as a backplane (ability #1)

var app = builder.Build();
app.MapHub<MyHub>("/myhub");
await app.RunAsync();

// A SignalR Hub. https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-7.0
class MyHub : Hub
{
}
```

### Silo configuration - grain storage

The SignalR.Orleans backplane (ability #1) uses grains under the hood that use storage to keep track of where each SignalR client is connected and what groups it belongs to. The storage used by default is `MemoryStorage`.

Use the given storage name constant to configure the correct storage provider.

```csharp
// **************************************************************************
// Use memory storage ONLY when your app is not clustered, otherwise you'll
// need to use proper external storage providers
// **************************************************************************
          
// Customize the storage used by the SignalR Orleans backplane grains.
siloBuilder.AddSomeOtherGrainStorage(SignalROrleansConstants.SIGNALR_ORLEANS_STORAGE_PROVIDER);

// THEN
siloBuilder.UseSignalR();
```

### Silo configuration - stream type and stream storage

SignalR.Orleans uses streams under the hood to provide the backplane (ability #1). The default stream type is `MemoryStream`. All streams in a given Orleans instance must use the same storage provider, named `PubSubStore`, currently defaulted to `MemoryStorage`.

```csharp
// FIRST customize the storage used by ALL stream providers in the entire Orleans host:
// Remember, memory storage won't work if you're using a cluster.
siloBuilder.AddSomeOtherGrainStorage("PubSubStore");

// THEN customize the type of stream used for the backplane, using the correct stream provider name
siloBuilder.AddPersistentStreams(SignalROrleansConstants.SIGNALR_ORLEANS_STREAM_PROVIDER, adapterFactory, configureStream);

// THEN
siloBuilder.UseSignalR();
```

## Sending messages from Orleans grains

If the SignalR app is cohosted as demonstrated above, you don't need this package to send messages from an Orleans grain. Simply inject `IHubContext<MyHub>` to the grain's constructor and call its methods to send messages. [Read more about it here.](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-7.0)

However, if the SignalR app is not cohosted, and if it's using Orleans as a backplane, then it's possible to use this package to send messages to the SignalR clients using the backplane streams in Orleans as a conduit (ability #2).

```csharp
class SampleGrain : Grain, ISampleGrain
{
  private readonly SignalR.Orleans.Core.HubContext<MyHub> _hubContext;

  public SampleGrain(SignalR.Orleans.Core.HubContext<MyHub> hubContext)
  {
    _hubContext = hubContext;
  }

  public async Task SendMessageToClients()
  {
    // Create an invocation message
    var msg = new InvocationMessage("method", new object?[]{ 1, 2, 3 }).ToImmutable();

    // Send a message to a single client
    await _hubContext.Client("someConnectionId").Send(msg);

    // Send a message to a group
    await _hubContext.Group("someGroupName").Send(msg);

    // Send a message to all connections made by a particular authenticated user
    await _hubContext.Group("someUserId").Send(msg);

    // TODO: We have not implemented ability to send a message to ALL clients yet.
  }
}
```

## Configuring the `IClusterClient`
If your SignalR app is cohosted with Orleans, it will automatically grab an `IClusterClient` from the service provider and connect to the Orleans backplane. 

However, if it's not cohosted, you'll have to give it an `IClusterClient` to use: 

```csharp
using Microsoft.AspNetCore.SignalR;
using Orleans.Hosting;
using SignalR.Orleans;

// Create a web application that will connect to a remote Orleans cluster
var builder = WebApplication.CreateBuilder(args);
builder.Services
    // Adds an IClusterClient to the service provider.
    .AddOrleansClient(clientBuilder =>
    {
        // Tell the client how to connect to Orleans (you'll need to customize this for yourself)
        clientBuilder.UseLocalhostClustering();
        // Tells the client how to connect to the SignalR.Orleans backplane.
        clientBuilder.UseSignalR(configure: null);
    })
    .AddSignalR()  // Adds SignalR hubs to the web application
    .AddOrleans(); // Tells SignalR to use Orleans as a backplane (ability #1)

var app = builder.Build();
app.MapHub<MyHub>("/myhub");
await app.RunAsync();

// A SignalR Hub. https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-7.0
class MyHub : Hub
{
}
```

This is the end of documentation for versions >= 7.0.0. Below is older documenation for previous versions.

---
# Earlier version documentation

## Configure the Silo
We need to configure the Orleans Silo with the below:
* Use `.UseSignalR()` on `ISiloHostBuilder`.
* Make sure to call `RegisterHub<THub>()` where `THub` is the type of the Hub you want to be added to the backplane.

***Example***
```cs
var silo = new SiloHostBuilder()
  .UseSignalR()
  .RegisterHub<MyHub>() // You need to call this per `Hub` type.
  .AddMemoryGrainStorage("PubSubStore") // You can use any other storage provider as long as you have one registered as "PubSubStore".
  .Build();

await silo.StartAsync();
```

### Configure Silo Storage Provider and Grain Persistance
Optional configuration to override the default implementation for both providers which by default are set as `Memory`.

***Example***
```cs
.UseSignalR(cfg =>
{
  cfg.Configure((builder, config) =>
  {
      builder
          .AddMemoryGrainStorage(config.PubSubProvider)
          .AddMemoryGrainStorage(config.StorageProvider);
  });
})
.RegisterHub<MyHub>()
```

## Client
Now your SignalR application needs to connect to the Orleans Cluster by using an Orleans Client:
* Use `.UseSignalR()` on `IClientBuilder`.

***Example***
```cs
var client = new ClientBuilder()
  .UseSignalR()
  // optional: .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences())
  .Build();

await client.Connect();
```

Somewhere in your `Startup.cs`:
* Add `IClusterClient` (created in the above example) to `IServiceCollection`.
* Use `.AddSignalR()` on `IServiceCollection` (this is part of `Microsoft.AspNetCore.SignalR` nuget package).
* Use `AddOrleans()` on `.AddSignalR()`.

***Example***
```cs
public void ConfigureServices(IServiceCollection services)
{
  ...
  services
    .AddSingleton<IClusterClient>(client)
    .AddSignalR()
    .AddOrleans();
  ...
}
```
Great! Now you have SignalR configured and Orleans SignalR backplane built in Orleans!

# Features
## Hub Context
`HubContext` gives you the ability to communicate with the client from orleans grains (outside the hub).

Sample usage: Receiving server push notifications from message brokers, web hooks, etc. Ideally first update your grain state and then push signalr message to the client.

### Example
```cs
public class UserNotificationGrain : Grain<UserNotificationState>, IUserNotificationGrain
{
  private HubContext<IUserNotificationHub> _hubContext;

  public override async Task OnActivateAsync()
  {
    _hubContext = GrainFactory.GetHub<IUserNotificationHub>();
    // some code...
    await _hubContext.User(this.GetPrimaryKeyString()).Send("Broadcast", State.UserNotification);
  }
}
```

# Complete examples

### Cohosting aspnetcore website and orleans

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

// Cohosting aspnetcore website and Orleans with signalR
var host = Host.CreateDefaultBuilder(args)

  // Add the webhost with SignalR configured.
  .ConfigureWebHostDefaults(webBuilder =>
  {
    webBuilder.ConfigureServices((webBuilderContext, services) =>
    {
      // Add response compression used by the SignalR hubs.
      services.AddResponseCompression(opts =>
      {
        opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/octet-stream" });
      });

      // Adds SignalR hubs to the aspnetcore website 
      services.AddSignalR(options =>
      {
      })
      .AddOrleans(); // Tells SignalR to use Orleans as the backplane.
    });

    webBuilder.Configure((ctx, app) =>
    {
      // Adds response compression for use by the SignalR hubs
      app.UseResponseCompression();
      
      // Map SignalR hub endpoints
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapHub<MyHubType1>("/hub1"); // use your own hub types
        endpoints.MapHub<MyHubType2>("/hub2"); // use your own hub types
        // ... etc
      });
    });
  })

  // Add Orleans with SignalR configured
  .UseOrleans((context, siloBuilder) =>
  {
    siloBuilder
      .UseSignalR(signalRConfig =>
      {
        // Optional.
        signalRConfig.UseFireAndForgetDelivery = true;

        signalRConfig.Configure((siloBuilder, signalRConstants) =>
        {
          // **************************************************************************
          // Use memory storage ONLY when your app is not clustered, otherwise you'll
          // need to use proper external storage providers
          // **************************************************************************

          siloBuilder.AddMemoryGrainStorage(signalRConstants.StorageProvider);
          // This wouldn't be be necessary if you already added "PubSubStore" elsewhere.
          siloBuilder.AddMemoryGrainStorage(signalRConstants.PubSubProvider /*Same as "PubSubStore"*/);
        });
      })

      // Allows Orleans grains to inject IHubContext<HubType>
      .RegisterHub<MyHubType1>()
      .RegisterHub<MyHubType2>();
      // ... etc
  })
  .UseConsoleLifetime()
  .Build();

await host.StartAsync();
await host.WaitForShutdownAsync(default);
```


# Contributions
PRs and feedback are **very** welcome!
