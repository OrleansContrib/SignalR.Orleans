// Copyright (c) .NET Foundation. All rights reserved. Licensed under the Apache License, Version
// 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans;
using SignalR.Orleans.Tests.Models;
using SignalR.Orleans.Tests.AspnetSignalR;
using Xunit;

namespace SignalR.Orleans.Tests
{
    public class OrleansHubLifetimeManagerTests : IClassFixture<OrleansFixture>
    {
        private readonly OrleansFixture _fixture;

        public OrleansHubLifetimeManagerTests(OrleansFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddGroupAsync_ForConnection_OnDifferentServer_AlreadyInGroup_SkipsDuplicate()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddToGroupAsync(connection.ConnectionId, "easye");
                await manager2.AddToGroupAsync(connection.ConnectionId, "easye");

                await manager2.SendGroupAsync("easye", "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.AddToGroupAsync(connection.ConnectionId, "ice-cube");

                await manager2.SendGroupAsync("ice-cube", "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task AddGroupAsync_ForLocalConnection_AlreadyInGroup_SkipsDuplicate()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddToGroupAsync(connection.ConnectionId, "dmx");
                await manager.AddToGroupAsync(connection.ConnectionId, "dmx");

                var grain = _fixture.ClientProvider.GetClient().GetGroupGrain("MyHub", "dmx");
                var result = await grain.Count();
                Assert.Equal(1, result);
            }
        }

        [Fact]
        public async Task DisconnectConnection_RemovesConnection_FromGroup()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.AddToGroupAsync(connection.ConnectionId, "dre");

                await manager.OnDisconnectedAsync(connection);

                var grain = _fixture.ClientProvider.GetClient().GetGroupGrain("MyHub", "dre");
                var result = await grain.Count();
                Assert.Equal(0, result);
            }
        }

        [Fact]
        public async Task Hub_InterfaceMatchingNaming_Output()
        {
            using (var client1 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<DaHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<DaHub>>(), _fixture.ClientProvider);

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
            }
        }

        [Fact]
        public async Task Hub_NonInterfaceMatchingNaming_Output()
        {
            using (var client1 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<DaHubx>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<DaHubx>>(), _fixture.ClientProvider);

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
            }
        }

        [Fact]
        public async Task HubUsingGenericBase_NonInterfaceMatchingNaming_Output()
        {
            using (var client1 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<DaHubUsingBase>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<DaHubUsingBase>>(), _fixture.ClientProvider);

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
            }
        }

        [Fact]
        public async Task InvokeAllAsync_DoesNotWriteTo_DisconnectedConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.OnDisconnectedAsync(connection2);

                await manager.SendAllAsync("Hello", new object[] { "World" });

                await AssertMessageAsync(client1);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_ForSpecificHub_WithMultipleServers_WritesTo_AllConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager3 = new OrleansHubLifetimeManager<DifferentHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<DifferentHub>>(), _fixture.ClientProvider);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client3.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);
                await manager3.OnConnectedAsync(connection3);

                await manager1.SendAllAsync("Hello", new object[] { "World" });

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
                Assert.Null(client3.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_WithMultipleServers_DoesNotWrite_ToDisconnectedConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager2.OnDisconnectedAsync(connection2);

                await manager2.SendAllAsync("Hello", new object[] { "World" });

                await AssertMessageAsync(client1);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeAllAsync_WithMultipleServers_WritesToAllConnections_Output()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager1.OnConnectedAsync(connection1);
                await manager2.OnConnectedAsync(connection2);

                await manager1.SendAllAsync("Hello", new object[] { "World" });

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeAllAsync_WritesTo_AllConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1).OrTimeout();
                await manager.OnConnectedAsync(connection2).OrTimeout();

                await manager.SendAllAsync("Hello", new object[] { "World" }).OrTimeout();

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeAsync_WhenNotConnectedAfterFailedAttemptsExceeds_ShouldForceDisconnect()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);

                var groupName = "flex";
                await manager.AddToGroupAsync(connection1.ConnectionId, groupName);
                await manager.AddToGroupAsync(connection2.ConnectionId, groupName);

                await manager.SendGroupAsync(groupName, "Hello", new object[] { "World" });

                var grain = _fixture.ClientProvider.GetClient().GetGroupGrain("MyHub", groupName);
                var connectionsCount = await grain.Count();

                await AssertMessageAsync(client1);
                Assert.Equal(2, connectionsCount);

                await manager.SendGroupAsync(groupName, "Hello", new object[] { "World" });
                await manager.SendGroupAsync(groupName, "Hello", new object[] { "World" });

                connectionsCount = await grain.Count();
                Assert.Equal(1, connectionsCount);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_ForLocalConnection_DoesNotPublish()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                // Add connection to both "servers" to see if connection receives message twice
                await manager1.OnConnectedAsync(connection);
                await manager2.OnConnectedAsync(connection);

                await manager1.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
                Assert.Null(client.TryRead());
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnNonExistentConnection_DoesNotThrow()
        {
            var invalidConnection = "NotARealConnectionId";
            var grain = _fixture.ClientProvider.GetClient().GetClientGrain("MyHub", invalidConnection);
            await grain.OnConnect(Guid.NewGuid());
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            await manager.SendConnectionAsync(invalidConnection, "Hello", new object[] { "World" });
        }

        [Fact]
        public async Task InvokeConnectionAsync_OnServer_WithoutConnection_WritesOutputTo_Connection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_WritesToConnection_Output()
        {
            using (var client = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.SendConnectionAsync(connection.ConnectionId, "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task InvokeConnectionAsync_WritesToConnections_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.SendConnectionsAsync(new string[] { connection1.ConnectionId, connection2.ConnectionId }, "Hello", new object[] { "World" });

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_OnServer_WithoutConnection_WritesOutputTo_GroupConnection()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddToGroupAsync(connection.ConnectionId, "tupac");

                await manager2.SendGroupAsync("tupac", "Hello", new object[] { "World" });

                await AssertMessageAsync(client);
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WhenOneDisconnected_ShouldDeliverOthers()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client3.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                var groupName = "gunit";
                await manager.AddToGroupAsync(connection1.ConnectionId, groupName);
                await manager.AddToGroupAsync(connection2.ConnectionId, groupName);
                await manager.AddToGroupAsync(connection3.ConnectionId, groupName);

                await manager.SendGroupAsync(groupName, "Hello", new object[] { "World" });

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroup_Except_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddToGroupAsync(connection1.ConnectionId, "gunit");
                await manager.AddToGroupAsync(connection2.ConnectionId, "gunit");

                await manager.SendGroupExceptAsync("gunit", "Hello", new object[] { "World" }, new string[] { connection2.ConnectionId });

                await AssertMessageAsync(client1);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroup_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddToGroupAsync(connection1.ConnectionId, "gunit");

                await manager.SendGroupAsync("gunit", "Hello", new object[] { "World" });

                await AssertMessageAsync(client1);

                Assert.Null(client2.TryRead());
            }
        }

        [Fact]
        public async Task InvokeGroupAsync_WritesTo_AllConnections_InGroups_Output()
        {
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            {
                var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);

                await manager.OnConnectedAsync(connection1);
                await manager.OnConnectedAsync(connection2);

                await manager.AddToGroupAsync(connection1.ConnectionId, "gunit");
                await manager.AddToGroupAsync(connection2.ConnectionId, "tupac");

                await manager.SendGroupsAsync(new string[] { "gunit", "tupac" }, "Hello", new object[] { "World" });

                await AssertMessageAsync(client1);
                await AssertMessageAsync(client2);
            }
        }

        [Fact]
        public async Task RemoveGroup_FromConnection_OnDifferentServer_NotInGroup_DoesNothing()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager2.RemoveFromGroupAsync(connection.ConnectionId, "does-not-exist-server");
            }
        }

        [Fact]
        public async Task RemoveGroup_FromLocalConnection_NotInGroup_DoesNothing()
        {
            var manager = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager.OnConnectedAsync(connection);

                await manager.RemoveFromGroupAsync(connection.ConnectionId, "does-not-exists");
            }
        }

        [Fact]
        public async Task RemoveGroupAsync_ForConnection_OnDifferentServer_Works()
        {
            var manager1 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);
            var manager2 = new OrleansHubLifetimeManager<MyHub>(new LoggerFactory().CreateLogger<OrleansHubLifetimeManager<MyHub>>(), _fixture.ClientProvider);

            using (var client = new TestClient())
            {
                var connection = HubConnectionContextUtils.Create(client.Connection);

                await manager1.OnConnectedAsync(connection);

                await manager1.AddToGroupAsync(connection.ConnectionId, "snoop");

                await manager2.SendGroupAsync("snoop", "Hello", new object[] { "World" });

                await AssertMessageAsync(client);

                await manager2.RemoveFromGroupAsync(connection.ConnectionId, "snoop");

                await manager2.SendGroupAsync("snoop", "Hello", new object[] { "World" });

                Assert.Null(client.TryRead());
            }
        }

        private async Task AssertMessageAsync(TestClient client)
        {
            var message = Assert.IsType<InvocationMessage>(await client.ReadAsync().OrTimeout());
            Assert.Equal("Hello", message.Target);
            Assert.Single(message.Arguments);
            Assert.Equal("World", message.Arguments[0].ToString());
        }
    }
}