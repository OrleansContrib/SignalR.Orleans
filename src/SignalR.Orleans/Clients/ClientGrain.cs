using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Clients
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class ClientState
    {
        private string DebuggerDisplay => $"ServerId: '{ServerId}'";

        public Guid ServerId { get; set; }
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class ClientGrain : Grain<ClientState>, IClientGrain
    {
        private readonly ILogger<ClientGrain> _logger;
        private IStreamProvider _streamProvider;
        private IAsyncStream<ClientMessage> _serverStream;
        private IAsyncStream<Guid> _serverDisconnectedStream;
        private IAsyncStream<string> _clientDisconnectStream;
        private ConnectionGrainKey _keyData;
        private StreamSubscriptionHandle<Guid> _serverDisconnectedSubscription;
        private const int _maxFailAttempts = 3;
        private int _failAttempts;

        public ClientGrain(ILogger<ClientGrain> logger)
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            _keyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            _clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, _keyData.Id);

            if (State.ServerId == Guid.Empty)
                return;

            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, Constants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(State.ServerId, Constants.SERVER_DISCONNECTED);
            var subscriptions = await _serverDisconnectedStream.GetAllSubscriptionHandles();
            var subscriptionTasks = new List<Task>();
            foreach (var subscription in subscriptions)
            {
                subscriptionTasks.Add(subscription.ResumeAsync(async (serverId, _) => await OnDisconnect("server-disconnected")));
            }
            await Task.WhenAll(subscriptionTasks);
        }

        public async Task Send(InvocationMessage message)
        {
            if (State.ServerId != Guid.Empty)
            {
                _logger.LogDebug("Sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Target, _keyData.Id);
                _failAttempts = 0;
                await _serverStream.OnNextAsync(new ClientMessage { ConnectionId = _keyData.Id, Payload = message, HubName = _keyData.HubName });
                return;
            }

            _logger.LogInformation("Client not connected for connectionId {connectionId} and hub {hubName} ({targetMethod})", _keyData.Id, _keyData.HubName, message.Target);

            _failAttempts++;
            if (_failAttempts >= _maxFailAttempts)
            {
                await OnDisconnect("attempts-limit-reached");
                _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                    _keyData.Id, _keyData.HubName, message.Target);
            }
        }

        public async Task OnConnect(Guid serverId)
        {
            State.ServerId = serverId;
            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, Constants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(State.ServerId, Constants.SERVER_DISCONNECTED);
            _serverDisconnectedSubscription = await _serverDisconnectedStream.SubscribeAsync(async _ => await OnDisconnect("server-disconnected"));
            await WriteStateAsync();
        }

        public async Task OnDisconnect(string reason = null)
        {
            _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via {reason}",
                _keyData.HubName, _keyData.Id, State.ServerId, reason);

            if (_keyData.Id != null)
            {
                await _clientDisconnectStream.OnNextAsync(_keyData.Id);
            }
            await ClearStateAsync();

            if (_serverDisconnectedSubscription != null)
                await _serverDisconnectedSubscription.UnsubscribeAsync();

            DeactivateOnIdle();
        }
    }
}