// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using SignalR.Orleans.Tests.Models;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Xunit;

namespace SignalR.Orleans.Tests
{
    public class OrleansHubLifetimeManagerTests : IClassFixture<OrleansFixture>
    {
        private readonly OrleansFixture _fixture;

        public OrleansHubLifetimeManagerTests(OrleansFixture fixture)
        {
            this._fixture = fixture;
        }

        [Fact]
        public async Task InvokeAllAsync_WritesTo_AllConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.SendAllAsync("Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());
                AssertMessage(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_DoesNotWriteTo_DisconnectedConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.OnDisconnectedAsync(connection2);

                await manager.SendAllAsync("Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroup_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit");

                await manager.SendGroupAsync("gunit", "Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroup_Except_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit");
                await manager.AddGroupAsync(connection2.ConnectionId, "gunit");

                await manager.SendGroupExceptAsync("gunit", "Hello", new object[] { "World" }, new string[] { connection2.ConnectionId });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroups_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit");
                await manager.AddGroupAsync(connection2.ConnectionId, "tupac");

                await manager.SendGroupsAsync(new string[] { "gunit", "tupac" }, "Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());
                AssertMessage(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_WritesToConnection_Output()
        {
            using (var client = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection = CreateHubConnectionContext(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_WritesToConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.SendConnectionsAsync(new string[] { connection1.ConnectionId, connection2.ConnectionId }, "Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());
                AssertMessage(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnNonExistentConnection_DoesNotThrow()
        {
            var invalidConnection = "NotARealConnectionId";
            var grain = this._fixture.Client.GetClientGrain("MyHub", invalidConnection);
            await grain.OnConnect(Guid.NewGuid(), "MyHub", invalidConnection);
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await manager.SendConnectionAsync(invalidConnection, "Hello", new object[] { "World" });
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnNonExistentConnection_WithoutCalling_OnConnect_ThrowsException()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SendConnectionAsync("NotARealConnectionIdV2", "Hello", new object[] { "World" }));
        }

        [Fact]
        public async Task InvokeAllAsync_WithMultipleServers_WritesToAllConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager1.SendAllAsync("Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());
                AssertMessage(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_WithMultipleServers_DoesNotWrite_ToDisconnectedConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager2.OnDisconnectedAsync(connection2);

                await manager2.SendAllAsync("Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();

                AssertMessage(client1.TryRead());

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnServer_WithoutConnection_WritesOutputTo_Connection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_OnServer_WithoutConnection_WritesOutputTo_GroupConnection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "tupac");

                await manager2.SendGroupAsync("tupac", "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
            }
        }

        [Fact]
        public async Task DisconnectConnection_RemovesConnection_FromGroup()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dre");

                await manager.OnDisconnectedAsync(connection);

                await connection.DisposeAsync();

                var grain = this._fixture.Client.GetGroupGrain("MyHub", "dre");
                var result = await grain.Count();
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public async Task RemoveGroup_FromLocalConnection_NotInGroup_DoesNothing()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.RemoveGroupAsync(connection.ConnectionId, "does-not-exists");

                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task RemoveGroup_FromConnection_OnDifferentServer_NotInGroup_DoesNothing()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.RemoveGroupAsync(connection.ConnectionId, "does-not-exist-server");

                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.AddGroupAsync(connection.ConnectionId, "ice-cube");

                await manager2.SendGroupAsync("ice-cube", "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForLocalConnection_AlreadyInGroup_SkipsDuplicate()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dmx");
                await manager.AddGroupAsync(connection.ConnectionId, "dmx");

                await connection.DisposeAsync();

                var grain = this._fixture.Client.GetGroupGrain("MyHub", "dmx");
                var result = await grain.Count();
                Assert.Equal(1, result);
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForConnection_OnDifferentServer_AlreadyInGroup_SkipsDuplicate()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "easye");
                await manager2.AddGroupAsync(connection.ConnectionId, "easye");

                await manager2.SendGroupAsync("easye", "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task RemoveGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "snoop");

                await manager2.SendGroupAsync("snoop", "Hello", new object[] { "World" });

                AssertMessage(await client.ReadAsync());

                await manager2.RemoveGroupAsync(connection.ConnectionId, "snoop");

                await manager2.SendGroupAsync("snoop", "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_ForLocalConnection_DoesNotPublish()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var connection = CreateHubConnectionContext(client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection);
                await manager2.OnConnectedAsync(connection);

                await manager1.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await connection.DisposeAsync();

                AssertMessage(client.TryRead());
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_ForSpecificHub_WithMultipleServers_WritesTo_AllConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager3 = new OrleansHubLifetimeManager<DifferentHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<DifferentHub>>(), this._fixture.Client);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var connection1 = CreateHubConnectionContext(client1.Connection);
                var connection2 = CreateHubConnectionContext(client2.Connection);
                var connection3 = CreateHubConnectionContext(client3.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);
                await manager3.OnConnectedAsync(connection3);

                await manager1.SendAllAsync("Hello", new object[] { "World" });

                await connection1.DisposeAsync();
                await connection2.DisposeAsync();
                await connection3.DisposeAsync();

                AssertMessage(client1.TryRead());
                AssertMessage(client2.TryRead());
                Assert.Null(client3.TryRead());
            }
        }

        private void AssertMessage(HubMessage item)
        {
            Assert.IsType<InvocationMessage>(item);
            var message = item as InvocationMessage;
            Assert.NotNull(message);
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }

        public static HubConnectionContext CreateHubConnectionContext(DefaultConnectionContext connection)
        {
            var context = new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance);
            context.ProtocolReaderWriter = new HubProtocolReaderWriter(new JsonHubProtocol(), new PassThroughEncoder());

            // Microsoft.AspNetCore.SignalR.Redis.Tests uses Microsoft.AspNetCore.SignalR.Tests.HubConnectionContextUtils 
            // which calls an internal method StartAsync. Reflection in a unittest! Huzzah!
            _ = _StartAsyncMethod.Invoke(context, Array.Empty<object>()); 

            return context;
        }

        private static readonly MethodInfo _StartAsyncMethod = typeof(HubConnectionContext).GetMethod("StartAsync", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}