// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using SignalR.Orleans.Tests.Models;
using System;
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.InvokeAllAsync("Hello", new object[] { "World" });

                AssertMessage(output1);
                AssertMessage(output2);
            }
        }

        [Fact]
        public async Task InvokeAllAsync_DoesNotWriteTo_DisconnectedConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.OnDisconnectedAsync(connection2);

                await manager.InvokeAllAsync("Hello", new object[] { "World" });

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroup_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddGroupAsync(connection1.ConnectionId, "gunit");

                await manager.InvokeGroupAsync("gunit", "Hello", new object[] { "World" });

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_WritesToConnection_Output()
        {
            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                AssertMessage(output);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnNonExistentConnection_DoesNotThrow()
        {
            var invalidConnection = "NotARealConnectionId";
            var grain = this._fixture.Client.GetClientGrain("MyHub", invalidConnection);
            await grain.OnConnect(Guid.NewGuid(), "MyHub", invalidConnection);
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await manager.InvokeConnectionAsync(invalidConnection, "Hello", new object[] { "World" });
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnNonExistentConnection_WithoutCalling_OnConnect_ThrowsException()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await Assert.ThrowsAsync<TimeoutException>(() => manager.InvokeConnectionAsync("NotARealConnectionIdV2", "Hello", new object[] { "World" }));
        }

        [Fact]
        public async Task InvokeAllAsync_WithMultipleServers_WritesToAllConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager1.InvokeAllAsync("Hello", new object[] { "World" });

                AssertMessage(output1);
                AssertMessage(output2);
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();

                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager2.OnDisconnectedAsync(connection2);

                await manager2.InvokeAllAsync("Hello", new object[] { "World" });

                AssertMessage(output1);

                Assert.False(output2.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnServer_WithoutConnection_WritesOutputTo_Connection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                AssertMessage(output);
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_OnServer_WithoutConnection_WritesOutputTo_GroupConnection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "tupac");

                await manager2.InvokeGroupAsync("tupac", "Hello", new object[] { "World" });

                AssertMessage(output);
            }
        }

        [Fact]
        public async Task DisconnectConnection_RemovesConnection_FromGroup()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dre");

                await manager.OnDisconnectedAsync(connection);

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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.RemoveGroupAsync(connection.ConnectionId, "does-not-exists");
            }
        }

        [Fact]
        public async Task RemoveGroup_FromConnection_OnDifferentServer_NotInGroup_DoesNothing()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.RemoveGroupAsync(connection.ConnectionId, "does-not-exist-server");
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.AddGroupAsync(connection.ConnectionId, "ice-cube");

                await manager2.InvokeGroupAsync("ice-cube", "Hello", new object[] { "World" });

                AssertMessage(output);
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForLocalConnection_AlreadyInGroup_SkipsDuplicate()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dmx");
                await manager.AddGroupAsync(connection.ConnectionId, "dmx");

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
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "easye");
                await manager2.AddGroupAsync(connection.ConnectionId, "easye");

                await manager2.InvokeGroupAsync("easye", "Hello", new object[] { "World" });

                AssertMessage(output);
                Assert.False(output.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task RemoveGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddGroupAsync(connection.ConnectionId, "snoop");

                await manager2.InvokeGroupAsync("snoop", "Hello", new object[] { "World" });

                AssertMessage(output);

                await manager2.RemoveGroupAsync(connection.ConnectionId, "snoop");

                await manager2.InvokeGroupAsync("snoop", "Hello", new object[] { "World" });

                Assert.False(output.In.TryRead(out var item));
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_ForLocalConnection_DoesNotPublish()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection);
                await manager2.OnConnectedAsync(connection);

                await manager1.InvokeConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                AssertMessage(output);
                Assert.False(output.In.TryRead(out var item));
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
                var output1 = Channel.CreateUnbounded<HubMessage>();
                var output2 = Channel.CreateUnbounded<HubMessage>();
                var output3 = Channel.CreateUnbounded<HubMessage>();

                var connection1 = new HubConnectionContext(output1, client1.Connection);
                var connection2 = new HubConnectionContext(output2, client2.Connection);
                var connection3 = new HubConnectionContext(output3, client3.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);
                await manager3.OnConnectedAsync(connection3);

                await manager1.InvokeAllAsync("Hello", new object[] { "World" });

                AssertMessage(output1);
                AssertMessage(output2);
                Assert.False(output3.In.TryRead(out var item));
            }
        }

        private void AssertMessage(Channel<HubMessage> channel)
        {
            Assert.True(channel.In.TryRead(out var item));
            var message = item as InvocationMessage;
            Assert.NotNull(message);
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }
    }
}