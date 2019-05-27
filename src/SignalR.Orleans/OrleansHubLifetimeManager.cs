using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using SignalR.Orleans.Clients;
using SignalR.Orleans.Core;

namespace SignalR.Orleans
{
    public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly ILogger _logger;
        private readonly IClusterClientProvider _clusterClientProvider;
        private readonly Guid _serverId;
        private IStreamProvider _streamProvider;
        private IAsyncStream<ClientMessage> _serverStream;
        private IAsyncStream<AllMessage> _allStream;
        private readonly string _hubName = typeof(THub).Name;

        public OrleansHubLifetimeManager(
            ILogger<OrleansHubLifetimeManager<THub>> logger,
            IClusterClientProvider clusterClientProvider
        )
        {
            _serverId = Guid.NewGuid();
            _logger = logger;
            _clusterClientProvider = clusterClientProvider;
        }

        private async Task SetupStreams()
        {
            _logger.LogInformation("Initializing: Orleans HubLifetimeManager {hubName} (serverId: {serverId})...", _hubName, _serverId);

            _streamProvider = _clusterClientProvider.GetClient().GetStreamProvider(Constants.STREAM_PROVIDER);
            _serverStream = _streamProvider.GetStream<ClientMessage>(_serverId, Constants.SERVERS_STREAM);
            _allStream = _streamProvider.GetStream<AllMessage>(Constants.ALL_STREAM_ID, Utils.BuildStreamHubName(_hubName));

            var subscribeTasks = new List<Task>
            {
                _allStream.SubscribeAsync((msg, token) => ProcessAllMessage(msg)),
                _serverStream.SubscribeAsync((msg, token) => ProcessServerMessage(msg))
            };

            await Task.WhenAll(subscribeTasks);

            _logger.LogInformation("Initialized complete: Orleans HubLifetimeManager {hubName} (serverId: {serverId})", _hubName, _serverId);
        }

        private Task ProcessAllMessage(AllMessage message)
        {
            var allTasks = new List<Task>(_connections.Count);
            var payload = (InvocationMessage)message.Payload;

            foreach (var connection in _connections)
            {
                if (connection.ConnectionAborted.IsCancellationRequested)
                    continue;

                if (message.ExcludedIds == null || !message.ExcludedIds.Contains(connection.ConnectionId))
                    allTasks.Add(SendLocal(connection, payload));
            }
            return Task.WhenAll(allTasks);
        }

        private Task ProcessServerMessage(ClientMessage message)
        {
            var connection = _connections[message.ConnectionId];
            if (connection == null) return Task.CompletedTask; // TODO: Log

            return SendLocal(connection, (InvocationMessage)message.Payload);
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            try
            {
                if (_streamProvider == null)
                {
                    await SetupStreams();
                }

                _connections.Add(connection);

                if (connection.User.Identity.IsAuthenticated)
                {
                    var user = _clusterClientProvider.GetClient().GetUserGrain(_hubName, connection.UserIdentifier);
                    await user.Add(connection.ConnectionId);
                }

                var client = _clusterClientProvider.GetClient().GetClientGrain(_hubName, connection.ConnectionId);
                await client.OnConnect(_serverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occurred 'OnConnectedAsync' while adding connection {connectionId} [hub: {hubName} (serverId: {serverId})]", connection?.ConnectionId, _hubName, _serverId);
                _connections.Remove(connection);
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            var client = _clusterClientProvider.GetClient().GetClientGrain(_hubName, connection.ConnectionId);
            await client.OnDisconnect();

            if (connection.User.Identity.IsAuthenticated)
            {
                //TODO: replace `connection.User.Identity.Name` with `connection.UserIdentifier` when next signalr will be published.
                var user = _clusterClientProvider.GetClient().GetUserGrain(_hubName, connection.User.Identity.Name);
                await user.Remove(connection.ConnectionId);
            }

            _connections.Remove(connection);
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = new CancellationToken())
        {
            var message = new InvocationMessage(methodName, args);
            return _allStream.OnNextAsync(new AllMessage { Payload = message });
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var message = new InvocationMessage(methodName, args);
            return _allStream.OnNextAsync(new AllMessage { Payload = message, ExcludedIds = excludedConnectionIds });
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(connectionId)) throw new ArgumentNullException(nameof(connectionId));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var message = new InvocationMessage(methodName, args);

            var connection = _connections[connectionId];
            if (connection != null) return SendLocal(connection, message);

            return SendExternal(connectionId, message);
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var tasks = connectionIds.Select(c => SendConnectionAsync(c, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var group = _clusterClientProvider.GetClient().GetGroupGrain(_hubName, groupName);
            return group.SendSignalRMessage(methodName, args);
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var tasks = groupNames.Select(g => SendGroupAsync(g, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentNullException(nameof(groupName));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var group = _clusterClientProvider.GetClient().GetGroupGrain(_hubName, groupName);
            var invocationMessage = new InvocationMessage(methodName, args);
            return group.SendMessageExcept(invocationMessage, excludedConnectionIds);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(methodName));

            var user = _clusterClientProvider.GetClient().GetUserGrain(_hubName, userId);
            return user.SendSignalRMessage(methodName, args);
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var tasks = userIds.Select(u => SendGroupAsync(u, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public override Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var group = _clusterClientProvider.GetClient().GetGroupGrain(_hubName, groupName);
            return group.Add(connectionId);
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var group = _clusterClientProvider.GetClient().GetGroupGrain(_hubName, groupName);
            return group.Remove(connectionId);
        }

        private Task SendLocal(HubConnectionContext connection, HubInvocationMessage hubMessage)
        {
            return connection.WriteAsync(hubMessage).AsTask();
        }

        private Task SendExternal(string connectionId, object hubMessage)
        {
            var client = _clusterClientProvider.GetClient().GetClientGrain(_hubName, connectionId);
            return client.SendMessage(hubMessage);
        }

        public void Dispose()
        {
            var toUnsubscribe = new List<Task>();
            if (_serverStream != null)
            {
                var subscriptions = _serverStream.GetAllSubscriptionHandles().Result;
                toUnsubscribe.AddRange(subscriptions.Select(s => s.UnsubscribeAsync()));
            }

            if (_allStream != null)
            {
                var subscriptions = _allStream.GetAllSubscriptionHandles().Result;
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