// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Core;
using SignalR.Orleans.Groups;
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
        public async Task InvokeAllAsyncWritesToAllConnectionsOutput()
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
        public async Task InvokeAllAsyncDoesNotWriteToDisconnectedConnectionsOutput()
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
        public async Task InvokeGroupAsyncWritesToAllConnectionsInGroupOutput()
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
        public async Task InvokeConnectionAsyncWritesToConnectionOutput()
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
        public async Task InvokeConnectionAsyncOnNonExistentConnectionDoesNotThrow()
        {
            var invalidConnection = "NotARealConnectionId";
            var grain = this._fixture.Client.GetGrain<IClientGrain>(Utils.BuildGrainName("MyHub", invalidConnection));
            await grain.OnConnect(Guid.NewGuid(), "MyHub", invalidConnection);
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await manager.InvokeConnectionAsync(invalidConnection, "Hello", new object[] { "World" });
        }

        [Fact]
        public async Task InvokeConnectionAsyncOnNonExistentConnectionWithoutCallingOnConnectThrowsException()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);
            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.InvokeConnectionAsync("NotARealConnectionId", "Hello", new object[] { "World" }));
        }

        [Fact]
        public async Task InvokeAllAsyncWithMultipleServersWritesToAllConnectionsOutput()
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
        public async Task InvokeAllAsyncWithMultipleServersDoesNotWriteToDisconnectedConnectionsOutput()
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
        public async Task InvokeConnectionAsyncOnServerWithoutConnectionWritesOutputToConnection()
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
        public async Task InvokeGroupAsyncOnServerWithoutConnectionWritesOutputToGroupConnection()
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
        public async Task DisconnectConnectionRemovesConnectionFromGroup()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dre");

                await manager.OnDisconnectedAsync(connection);

                var grain = this._fixture.Client.GetGrain<IGroupGrain>(Utils.BuildGrainName("MyHub", "dre"));
                var result = await grain.Count();
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public async Task RemoveGroupFromLocalConnectionNotInGroupDoesNothing()
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
        public async Task RemoveGroupFromConnectionOnDifferentServerNotInGroupDoesNothing()
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
        public async Task AddGroupAsyncForConnectionOnDifferentServerWorks()
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
        public async Task AddGroupAsyncForLocalConnectionAlreadyInGroupDoesNothing()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), this._fixture.Client);

            using (var client = new TestClient())
            {
                var output = Channel.CreateUnbounded<HubMessage>();

                var connection = new HubConnectionContext(output, client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddGroupAsync(connection.ConnectionId, "dmx");
                await manager.AddGroupAsync(connection.ConnectionId, "dmx");

                var grain = this._fixture.Client.GetGrain<IGroupGrain>(Utils.BuildGrainName("MyHub", "dmx"));
                var result = await grain.Count();
                Assert.Equal(1, result);
            }
        }

        [Fact]
        public async Task AddGroupAsyncForConnectionOnDifferentServerAlreadyInGroupDoesNothing()
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
        public async Task RemoveGroupAsyncForConnectionOnDifferentServerWorks()
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
        public async Task InvokeConnectionAsyncForLocalConnectionDoesNotPublishToRedis()
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

        private void AssertMessage(Channel<HubMessage> channel)
        {
            Assert.True(channel.In.TryRead(out var item));
            var message = item as InvocationMessage;
            Assert.NotNull(message);
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", (string)message.Arguments[0]);
        }

        private class MyHub : Hub
        {

        }
    }
}
