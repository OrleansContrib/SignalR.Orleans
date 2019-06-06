﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    internal abstract class ConnectionGrain<TGrainState> : Grain<TGrainState>, IConnectionGrain
        where TGrainState : ConnectionState, new()
    {
        private readonly ILogger _logger;
        private IStreamProvider _streamProvider;

        protected ConnectionGrainKey KeyData;

        internal ConnectionGrain(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            KeyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            var subscriptionTasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connection.Key);
                var subscriptions = await clientDisconnectStream.GetAllSubscriptionHandles();
                foreach (var subscription in subscriptions)
                {
                    subscriptionTasks.Add(subscription.ResumeAsync(async (connectionId, _) => await Remove(connectionId)));
                }
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public virtual async Task Add(string connectionId)
        {
            if (State.Connections.ContainsKey(connectionId))
                return;

            var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
            var subscription = await clientDisconnectStream.SubscribeAsync(async (connId, _) => await Remove(connId));
            State.Connections.Add(connectionId, subscription);
            await WriteStateAsync();
        }

        public virtual async Task Remove(string connectionId)
        {
            if (State.Connections.TryGetValue(connectionId, out var stream))
            {
                await stream.UnsubscribeAsync();
                State.Connections.Remove(connectionId);
            }
            if (State.Connections.Count == 0)
            {
                await ClearStateAsync();
                DeactivateOnIdle();
            }
            else
            {
                await WriteStateAsync();
            }
        }

        public virtual Task Send(InvocationMessage message)
        {
            _logger.LogDebug("Sending message to {hubName}.{targetMethod} on group {groupId} to {connectionsCount} connection(s)",
                KeyData.HubName, message.Target, KeyData.Id, State.Connections.Count);

            var tasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                var client = GrainFactory.GetClientGrain(KeyData.HubName, connection.Key);
                tasks.Add(client.Send(message));
            }

            return Task.WhenAll(tasks);
        }

        public Task SendExcept(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var message = new InvocationMessage(methodName, args);
            var tasks = new List<Task>();
            foreach (var connection in State.Connections)
            {
                if (excludedConnectionIds.Contains(connection.Key)) continue;

                var client = GrainFactory.GetClientGrain(KeyData.HubName, connection.Key);
                tasks.Add(client.Send(message));
            }

            return Task.WhenAll(tasks);
        }

        public Task<int> Count()
        {
            return Task.FromResult(State.Connections.Count);
        }
    }

    internal abstract class ConnectionState
    {
        public Dictionary<string, StreamSubscriptionHandle<string>> Connections { get; set; } = new Dictionary<string, StreamSubscriptionHandle<string>>();
    }
}