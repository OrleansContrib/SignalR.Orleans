using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Streams;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Groups;
using SignalR.Orleans.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Orleans
{
    public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
    {
        private readonly HubConnectionList _connections = new HubConnectionList();
        private ILogger _logger;
        private readonly IClusterClient _clusterClient;
        private readonly Guid _serverId;
        private IStreamProvider _streamProvider;
        private IAsyncStream<ClientMessage> _serverStream;
        private IAsyncStream<AllMessage> _allStream;
        private readonly string _hubTypeName = typeof(THub).FullName.Replace('+', '.');
        private const string UserGroupPrefix = "user::";

        private readonly JsonSerializer _serializer = new JsonSerializer
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.None
        };

        public OrleansHubLifetimeManager(
            ILogger<OrleansHubLifetimeManager<THub>> logger,
            IClusterClient clusterClient)
        {
            _serverId = Guid.NewGuid();
            this._logger = logger;
            this._clusterClient = clusterClient;

            this.SetupStreams().Wait();
        }

        private async Task SetupStreams()
        {
            this._streamProvider = this._clusterClient.GetStreamProvider(Constants.STREAM_PROVIDER);
            this._serverStream = this._streamProvider.GetStream<ClientMessage>(_serverId, Constants.SERVERS_STREAM);
            this._allStream = this._streamProvider.GetStream<AllMessage>(Constants.ALL_STREAM_ID, this._hubTypeName);

            var subscribeTasks = new List<Task>();
            var allStreamHandlers = await _allStream.GetAllSubscriptionHandles();
            if (allStreamHandlers != null && allStreamHandlers.Count > 0)
            {
                foreach (var handler in allStreamHandlers)
                {
                    subscribeTasks.Add(handler.ResumeAsync((msg, token) => this.ProcessAllMessage(msg)));
                }
            }
            else
            {
                subscribeTasks.Add(this._allStream.SubscribeAsync((msg, token) => this.ProcessAllMessage(msg)));
            }

            var serverStreamHandlers = await _serverStream.GetAllSubscriptionHandles();
            if (serverStreamHandlers != null && serverStreamHandlers.Count > 0)
            {
                foreach (var handler in serverStreamHandlers)
                {
                    subscribeTasks.Add(handler.ResumeAsync((msg, token) => this.ProcessServerMessage(msg)));
                }
            }
            else
            {
                subscribeTasks.Add(this._serverStream.SubscribeAsync((msg, token) => this.ProcessServerMessage(msg)));
            }
        }

        private Task ProcessAllMessage(AllMessage message)
        {
            var allTasks = new List<Task>(this._connections.Count);
            var payload = (InvocationMessage)message.Payload;

            foreach (var connection in this._connections)
            {
                if (connection.ConnectionAbortedToken != null &&
                    connection.ConnectionAbortedToken.IsCancellationRequested)
                    continue;

                if (message.ExcludedIds == null || !message.ExcludedIds.Contains(connection.ConnectionId))
                    allTasks.Add(this.InvokeLocal(connection, payload));
            }
            return Task.WhenAll(allTasks);
        }

        private Task ProcessServerMessage(ClientMessage message)
        {
            var connection = this._connections[message.ConnectionId];
            if (connection == null) return Task.CompletedTask; // TODO: Log

            return this.InvokeLocal(connection, (InvocationMessage)message.Payload);
        }

        public override Task AddGroupAsync(string connectionId, string groupName)
        {
            var group = this._clusterClient.GetGrain<IGroupGrain>(groupName);
            return group.AddMember(connectionId);
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            var message = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: args);
            return this._allStream.OnNextAsync(new AllMessage { Payload = message });
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            var message = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: args);
            return this._allStream.OnNextAsync(new AllMessage { Payload = message, ExcludedIds = excludedIds });
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentNullException(nameof(connectionId));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var message = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: args);

            var connection = this._connections[connectionId];
            if (connection != null) return InvokeLocal(connection, message);

            return InvokeExternal(connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var message = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: args);
            var group = this._clusterClient.GetGrain<IGroupGrain>(groupName);
            return group.SendMessage(message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var message = new InvocationMessage(Guid.NewGuid().ToString(), nonBlocking: true, target: methodName, arguments: args);
            var user = this._clusterClient.GetGrain<IUserGrain>(userId);
            return user.SendMessage(message);
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            try
            {
                this._connections.Add(connection);

                if (connection.User.Identity.IsAuthenticated)
                {
                    //TODO: Switch `connection.User.Identity.Name` with `connection.UserIdentifier` when next signalr will be published.
                    var user = this._clusterClient.GetGrain<IUserGrain>(connection.User.Identity.Name);
                    await user.AddMember(connection.ConnectionId);
                }

                var client = this._clusterClient.GetGrain<IClientGrain>(connection.ConnectionId);
                await client.OnConnect(this._serverId);
            }
            catch (Exception exc)
            {
                this._connections.Remove(connection);
                throw exc;
            }
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            var client = this._clusterClient.GetGrain<IClientGrain>(connection.ConnectionId);
            await client.OnDisconnect();

            if (connection.User.Identity.IsAuthenticated)
            {
                //TODO: Switch `connection.User.Identity.Name` with `connection.UserIdentifier` when next signalr will be published.
                var user = this._clusterClient.GetGrain<IUserGrain>(connection.User.Identity.Name);
                await user.RemoveMember(connection.ConnectionId);
            }

            this._connections.Remove(connection);
        }

        public override Task RemoveGroupAsync(string connectionId, string groupName)
        {
            var group = this._clusterClient.GetGrain<IGroupGrain>(groupName);
            return group.RemoveMember(connectionId);
        }

        private async Task InvokeLocal(HubConnectionContext connection, HubMessage hubMessage)
        {
            while (await connection.Output.WaitToWriteAsync())
            {
                if (connection.Output.TryWrite(hubMessage))
                {
                    break;
                }
            }
        }

        private Task InvokeExternal(string connectionId, object hubMessage)
        {
            var client = this._clusterClient.GetGrain<IClientGrain>(connectionId);
            return client.SendMessage(hubMessage);
        }

        public void Dispose()
        {
            var toUnsubscribe = new List<Task>();
            if (this._serverStream != null)
            {
                var subscriptions = this._serverStream.GetAllSubscriptionHandles().Result;
                toUnsubscribe.AddRange(subscriptions.Select(s => s.UnsubscribeAsync()));
            }

            if (this._allStream != null)
            {
                var subscriptions = this._allStream.GetAllSubscriptionHandles().Result;
                toUnsubscribe.AddRange(subscriptions.Select(s => s.UnsubscribeAsync()));
            }

            Task.WaitAll(toUnsubscribe.ToArray());
        }
    }

    public class AllMessage
    {
        public IReadOnlyList<string> ExcludedIds { get; set; }
        public object Payload { get; set; }
    }
}
