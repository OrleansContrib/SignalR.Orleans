using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Concurrency;
using SignalR.Orleans.Core;
using System.Diagnostics;

namespace SignalR.Orleans.Clients
{
    /// <inheritdoc cref="IClientGrain"/>
    internal sealed class ClientGrain : Grain, IClientGrain
    {
        private const string CLIENT_STORAGE = "ClientState";
        private const int MAX_FAIL_ATTEMPTS = 3;

        private readonly ILogger<ClientGrain> _logger;
        private readonly IPersistentState<ClientGrainState> _clientState;

        private string _hubName = default!;
        private string _connectionId = default!;
        private Guid _serverId => _clientState.State.ServerId;

        private IStreamProvider _streamProvider = default!;
        private StreamSubscriptionHandle<Guid>? _serverDisconnectedSubscription = default;

        private int _failAttempts = 0;

        public ClientGrain(
            ILogger<ClientGrain> logger,
            [PersistentState(CLIENT_STORAGE, Constants.STORAGE_PROVIDER)] IPersistentState<ClientGrainState> clientState)
        {
            _logger = logger;
            _clientState = clientState;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var key = ClientKey.FromGrainPrimaryKey(this.GetPrimaryKeyString());
            _hubName = key.HubType;
            _connectionId = key.ConnectionId;

            _streamProvider = this.GetOrleansSignalRStreamProvider();

            // Resume subscriptions if we have already been "connected".
            // We know we have already been connected if the "ServerId" parameter is set.
            if (_serverId != default)
            {
                // We will listen to this stream to know if the server is disconnected (silo goes down) so that we can enact client disconnected procedure.
                var serverDisconnectedStream = _streamProvider.GetServerDisconnectionStream(_clientState.State.ServerId);
                var _serverDisconnectedSubscription = (await serverDisconnectedStream.GetAllSubscriptionHandles())[0];
                await _serverDisconnectedSubscription.ResumeAsync((serverId, _) => OnDisconnect("server-disconnected"));
            }
        }

        public async Task OnConnect(Guid serverId)
        {
            var serverDisconnectedStream = _streamProvider.GetServerDisconnectionStream(serverId);
            _serverDisconnectedSubscription = await serverDisconnectedStream.SubscribeAsync(_ => OnDisconnect("server-disconnected"));

            _clientState.State.ServerId = serverId;
            await _clientState.WriteStateAsync();
        }

        public async Task OnDisconnect(string? reason = null)
        {
            _logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via reason '{reason}'.",
                _hubName, _connectionId, _clientState.State.ServerId, reason);

            if (_serverDisconnectedSubscription is not null)
            {
                await _serverDisconnectedSubscription.UnsubscribeAsync();
                _serverDisconnectedSubscription = null;
            }

            await _streamProvider.GetClientDisconnectionStream(_connectionId).OnNextAsync(_connectionId);

            await _clientState.ClearStateAsync();

            DeactivateOnIdle();
        }

        // NB: Interface method is marked [ReadOnly] so this method will be re-entrant/interleaved.
        public async Task Send(Immutable<InvocationMessage> message)
        {
            if (_serverId != default)
            {
                _logger.LogDebug("Sending message on {hubName}.{message.Value.Target} to connection {connectionId}",
                    _hubName, message.Value.Target, _connectionId);

                // Routes the message to the silo (server) where the client is actually connected.
                await _streamProvider.GetServerStream(_serverId).OnNextAsync(new ClientMessage(_hubName, _connectionId, message));

                Interlocked.Exchange(ref _failAttempts, 0);
            }
            else
            {
                _logger.LogInformation("Client not connected for connectionId '{connectionId}' and hub '{hubName}' ({targetMethod})",
                    _connectionId, _hubName, message.Value.Target);

                if (Interlocked.Increment(ref _failAttempts) >= MAX_FAIL_ATTEMPTS)
                {
                    _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                        _connectionId, _hubName, message.Value.Target);

                    await OnDisconnect("attempts-limit-reached");
                }
            }
        }
    }
}