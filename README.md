<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="SignalR.Orleans" width="600px"> 
  <h1>SignalR.Orleans</h1>
</p>

[![Build status](https://projectappengine.visualstudio.com/_apis/public/build/definitions/66fe6898-2b40-410a-b05d-893a610d2ccb/1/badge)](https://projectappengine.visualstudio.com/_apis/public/build/definitions/66fe6898-2b40-410a-b05d-893a610d2ccb/1/badge)
[![NuGet](https://img.shields.io/nuget/v/SignalR.Orleans.svg?style=flat)](http://www.nuget.org/profiles/SignalR.Orleans)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dotnet/orleans?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[ASP.NET Core SignalR](https://github.com/aspnet/SignalR) is a new library for ASP.NET Core developers that makes it incredibly simple to add real-time web functionality to your applications. What is "real-time web" functionality? It's the ability to have your server-side code push content to the connected clients as it happens, in real-time.

**SignalR.Orleans** is a package that allow us to enhance the _real-time_ capabilities of SignalR by leveraging Orleans distributed cloud platform capabilities.


Installation
============

Installation is performed via [NuGet](https://www.nuget.org/packages/SignalR.Orleans/)

From Package Manager:

> Install-Package SignalR.Orleans -Version 1.0.0-preview-1

.Net CLI:

> dotnet add package SignalR.Orleans --version 1.0.0-preview-1

Packet: 

> paket add SignalR.Orleans --version 1.0.0-preview-1


Code Examples
=============

At SignalR application configuration (`ISignalRBuilder`):

```c#
signalRBuilder.AddOrleans();
```

In your Orleans Client configuration (`ClientConfiguration`):

```c#
clientConfiguration.AddSignalR();
```

In your Orleans client builder (`IClientBuilder`):

```c#
clientBuilder.UseSignalR();
```

In your Orleans silo builder (`ISiloHostBuilder`):

```c#
siloBuilder..UseSignalR();
```

In your Orleans cluster configuration (`ClusterConfiguration`):

```c#
clusterConfiguration.AddSignalR();
```

PRs and feedback is **very** welcome!