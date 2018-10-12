<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="SignalR.Orleans" width="300px"> 
  <h1>SignalR.Orleans</h1>
</p>

[![Build status](https://dotnet-ci.visualstudio.com/DotnetCI/_apis/build/status/SignalR-Orleans-CI?branch=master)](https://dotnet-ci.visualstudio.com/DotnetCI/_build/latest?definitionId=1&branch=master)
[![NuGet](https://img.shields.io/nuget/v/SignalR.Orleans.svg?style=flat)](http://www.nuget.org/packages/SignalR.Orleans)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[ASP.NET Core SignalR](https://github.com/aspnet/SignalR) is a new library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

**SignalR.Orleans** is a package that allow us to enhance the _real-time_ capabilities of SignalR by leveraging Orleans distributed cloud platform capabilities.


# Installation

Installation is performed via [NuGet](https://www.nuget.org/packages/SignalR.Orleans/)

From Package Manager:

> PS> Install-Package SignalR.Orleans -prerelease

.Net CLI:

> \# dotnet add package SignalR.Orleans -prerelease

Paket: 

> \# paket add SignalR.Orleans -prerelease

# Configuration

## Silo
We need to configure the Orleans Silo with the below:
* Use `.UseSignalR()` on `ISiloHostBuilder`.

***Example***
```cs
var siloPort = 11111;
int gatewayPort = 30000;
var siloAddress = IPAddress.Loopback;

var silo = new SiloHostBuilder()
    .UseSignalR()
    .Build();
await silo.StartAsync();
```

## Client
Now your SignalR application needs to connect to the Orleans Cluster by using an Orleans Client:
* Use `.UseSignalR()` on `IClientBuilder`.

***Example***
```cs
var client = new ClientBuilder()
    .UseSignalR()
    .Build();
    await client.Connect();
```

Somewhere in your `Startup.cs`:
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
