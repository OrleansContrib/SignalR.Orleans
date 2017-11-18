<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="SignalR.Orleans" width="300px"> 
  <h1>SignalR.Orleans</h1>
</p>

[![Build status](https://projectappengine.visualstudio.com/_apis/public/build/definitions/66fe6898-2b40-410a-b05d-893a610d2ccb/1/badge)](https://projectappengine.visualstudio.com/_apis/public/build/definitions/66fe6898-2b40-410a-b05d-893a610d2ccb/1/badge)
[![NuGet](https://img.shields.io/nuget/v/SignalR.Orleans.svg?style=flat)](http://www.nuget.org/profiles/SignalR.Orleans)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[ASP.NET Core SignalR](https://github.com/aspnet/SignalR) is a new library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

**SignalR.Orleans** is a package that allow us to enhance the _real-time_ capabilities of SignalR by leveraging Orleans distributed cloud platform capabilities.


# Installation

Installation is performed via [NuGet](https://www.nuget.org/packages/SignalR.Orleans/)

From Package Manager:

> PS> Install-Package SignalR.Orleans -Version 1.0.0-preview-1

.Net CLI:

> \# dotnet add package SignalR.Orleans --version 1.0.0-preview-1

Packet: 

> \# packet add SignalR.Orleans --version 1.0.0-preview-1

# Configuration

## Silo
First we need to have an Orleans cluster up and running.

```cs
var siloConfig = ClusterConfiguration.LocalhostPrimarySilo()
    .AddSignalR();

var silo = new SiloHostBuilder()
    .UseConfiguration(siloConfig)
    .UseSignalR()
    .Build();
await silo.StartAsync();
```

## Client
Now your SignalR aplication needs to connect to the Orleans Cluster by using an Orleans Client.

```cs
var clientConfig = ClientConfiguration.LocalhostSilo()
    .AddSignalR();

var client = new ClientBuilder()
    .UseConfiguration(clientConfig)
    .UseSignalR()
    .Build();
await client.Connect();
```

Somewhere in your `Startup.cs`:

```cs
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddSignalR()
            .AddOrleans(client);
    ...
}
```
Great! Now you have an Orleans backplane built in Orleans!

# Features
## Hub Context
`HubContext` gives you the ability to communicate with the client from orleans grains (outside the hub).

Sample usage: Receiving server push notifications from message brokers, web hooks, etc. Ideally first update your grain state and then push signalr message to the client.

### Example: 
```cs
public class UserNotificationGrain : Grain<UserNotificationState>, IUserNotificationGrain
{
	private HubContext<IUserNotificationHub> _hubContext;

	public override async Task OnActivateAsync()
	{
		_hubContext = GrainFactory.GetHub<IUserNotificationHub>();
		// some code...
		await _hubContext.User(this.GetPrimaryKeyString()).SendSignalRMessage("Broadcast", State.UserNotification);
	}
}
```

# Contributions
PRs and feedback are **very** welcome!
