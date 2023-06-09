<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="SignalR.Orleans" width="300px"> 
  <h1>Sketch7 SignalR.Orleans</h1><small>fork from <a href="https://github.com/OrleansContrib/SignalR.Orleans">OrleansContrib/SignalR.Orleans</a></small>
</p>

[![Package build/publish](https://github.com/sketch7/SignalR.Orleans/actions/workflows/dotnet-publish.yml/badge.svg)](https://github.com/sketch7/SignalR.Orleans/actions/workflows/dotnet-publish.yml)
[![NuGet version](https://badge.fury.io/nu/Sketch7.SignalR.Orleans.svg)](https://badge.fury.io/nu/Sketch7.SignalR.Orleans)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[ASP.NET Core SignalR](https://github.com/aspnet/SignalR) is a new library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

**Sketch7.SignalR.Orleans** is a package that allow us to enhance the _real-time_ capabilities of SignalR by leveraging Orleans distributed cloud platform capabilities.


# Installation

Installation is performed via [NuGet](https://www.nuget.org/packages/Sketch7.SignalR.Orleans/)

## dotnet support
- Use 1.x for netcore2
- Use 2.x for netcore3
- Use 3.x for net5
- Use 4.x for net6

From Package Manager:

> PS> Install-Package Sketch7.SignalR.Orleans
> PS> Install-Package Sketch7.SignalR.Orleans.AspNet

.Net CLI:

> \# dotnet add package Sketch7.SignalR.Orleans
> \# dotnet add package Sketch7.SignalR.Orleans.AspNet

Paket:

> \# paket add Sketch7.SignalR.Orleans
> \# paket add Sketch7.SignalR.Orleans.AspNet

# Configuration

## Silo
We need to configure the Orleans Silo with the below:
* Use `.UseSignalR()` on `ISiloHostBuilder`.

***Example***
```cs
new HostBuilder()
  .UseOrleans((context, siloBuilder) =>
  {
    siloBuilder
      .AddMemoryGrainStorage("PubSubStore") // You can use any other storage provider as long as you have one registered as "PubSubStore".
      .UseSignalR();
  });
```

### Configure Silo Storage Provider and Grain Persistance
Optional configuration to override the default implementation for both providers which by default are set as `Memory`.

***Example***
```cs
.UseSignalR(signalrBuilder => signalrBuilder.Configure((sb, opts) =>
{
  siloBuilder.AddMemoryGrainStorage(opts.StorageProvider);
}));
```

## Client
Now your SignalR application needs to connect to the Orleans Cluster by using an Orleans Client:
* Use `.UseSignalR()` on `IClientBuilder`.

***Example***
```cs
			var hostBuilder = new HostBuilder()
				.UseOrleans((ctx, builder) =>
				{
					builder.UseSignalR()
				});

hostBuilder.RunConsoleAsync();
```

Somewhere in your `Startup.cs`:
* Add the following in `ConfigureServices`.
* Use `.AddSignalR()` on `IServiceCollection` (this is part of `Microsoft.AspNetCore.SignalR` nuget package).
* Use `AddOrleans()` on `.AddSignalR()`.

***Example***
```cs
public void ConfigureServices(IServiceCollection services)
{
  ...
  services
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

# Contributions
PRs and feedback are **very** welcome!