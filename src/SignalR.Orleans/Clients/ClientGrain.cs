using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Concurrency;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Clients
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class ClientState
    {
        private string DebuggerDisplay => $"ServerId: '{ServerId}'";

        public Guid ServerId { get; set; }
    }

    [Reentrant]
    internal class ClientGrain : Grain, IClientGrain
    {
        private const string CLIENT_STORAGE = "ClientState";
        private readonly ILogger<ClientGrain> _logger;
        private readonly IPersistentState<ClientState> _clientState;
        private IStreamProvider _streamProvider = default!;
        private IAsyncStream<ClientMessage> _serverStream = default!;
        private IAsyncStream<Guid> _serverDisconnectedStream = default!;
        private IAsyncStream<string> _clientDisconnectStream = default!;
        private ConnectionGrainKey _keyData = default!;
        private StreamSubscriptionHandle<Guid>? _serverDisconnectedSubscription;
        private const int _maxFailAttempts = 3;
        private int _failAttempts;

        public ClientGrain(
            ILogger<ClientGrain> logger,
            [PersistentState(CLIENT_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<ClientState> clientState)
        {
            _logger = logger;
            _clientState = clientState;
        }

        public override async Task OnActivateAsync()
        {
            _keyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            _clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, _keyData.Id);

            if (_clientState.State.ServerId == Guid.Empty)
                return;

            _serverStream = _streamProvider.GetStream<ClientMessage>(_clientState.State.ServerId, Constants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(_clientState.State.ServerId, Constants.SERVER_DISCONNECTED);
            var subscriptions = await _serverDisconnectedStream.GetAllSubscriptionHandles();
            var subscriptionTasks = new Task[subscriptions.Count];
            for (int i = 0; i < subscriptions.Count; i++)
            {
                var subscription = subscriptions[i];
                subscriptionTasks[i] = subscription.ResumeAsync((serverId, _) => OnDisconnect("server-disconnected"));
            }

            await Task.WhenAll(subscriptionTasks);
        }

        public async Task Send(Immutable<InvocationMessage> message)
        {
            if (_clientState.State.ServerId != Guid.Empty)
            {
                _logger.LogDebug("Sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Value.Target, _keyData.Id);
                _failAttempts = 0;
                await _serverStream.OnNextAsync(new ClientMessage(_keyData.HubName, _keyData.Id, message.Value));
                return;
            }

            _logger.LogInformation("Client not connected for connectionId {connectionId} and hub {hubName} ({targetMethod})", _keyData.Id, _keyData.HubName, message.Value.Target);

            _failAttempts++;
            if (_failAttempts >= _maxFailAttempts)
            {
                await OnDisconnect("attempts-limit-reached");
                _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                    _keyData.Id, _keyData.HubName, message.Value.Target);
            }
        }

        public async Task OnConnect(Guid serverId)
        {
            _clientState.State.ServerId = serverId;
            _serverStream = _streamProvider.GetStream<ClientMessage>(_clientState.State.ServerId, Constants.SERVERS_STREAM);
            _serverDisconnectedStream = _streamProvider.GetStream<Guid>(_clientState.State.ServerId, Constants.SERVER_DISCONNECTED);
            _serverDisconnectedSubscription = await _serverDisconnectedStream.SubscribeAsync(_ => OnDisconnect("server-disconnected"));
            await _clientState.WriteStateAsync();
        }

        public async Task OnDisconnect(string? reason = null)
        {
            _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via {reason}",
                _keyData.HubName, _keyData.Id, _clientState.State.ServerId, reason);

            if (_keyData.Id != null)
            {
                await _clientDisconnectStream.OnNextAsync(_keyData.Id);
            }
            await _clientState.ClearStateAsync();

            if (_serverDisconnectedSubscription != null)
                await _serverDisconnectedSubscription.UnsubscribeAsync();

            DeactivateOnIdle();
        }
    }
}