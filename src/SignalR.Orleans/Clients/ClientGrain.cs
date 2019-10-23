using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
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
        private IAsyncStream<string> _clientDisconnectStream;
        private ConnectionGrainKey _keyData;
        private const int _maxFailAttempts = 3;
        private int _failAttempts;

        public ClientGrain(ILogger<ClientGrain> logger)
        {
            _logger = logger;
        }

        public override Task OnActivateAsync()
        {
            _keyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            _clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, _keyData.Id);

            if (State.ServerId == Guid.Empty)
                return Task.CompletedTask;

            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, Constants.SERVERS_STREAM);
            return Task.CompletedTask;
        }

        public async Task Send(Immutable<InvocationMessage> message)
        {
            if (State.ServerId != Guid.Empty)
            {
                _logger.LogDebug("Sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Value.Target, _keyData.Id);
                _failAttempts = 0;
                await _serverStream.OnNextAsync(new ClientMessage { ConnectionId = _keyData.Id, Payload = message.Value, HubName = _keyData.HubName });
                return;
            }

            _logger.LogInformation("Client not connected for connectionId {connectionId} and hub {hubName} ({targetMethod})", _keyData.Id, _keyData.HubName, message.Value.Target);

            _failAttempts++;
            if (_failAttempts >= _maxFailAttempts)
            {
                await OnDisconnect();
                _logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit",
                    _keyData.Id, _keyData.HubName, message.Value.Target);
            }
        }

        public Task OnConnect(Guid serverId)
        {
            State.ServerId = serverId;
            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, Constants.SERVERS_STREAM);
            return WriteStateAsync();
        }

        public async Task OnDisconnect()
        {
            if (_keyData.Id != null)
            {
                await _clientDisconnectStream.OnNextAsync(_keyData.Id);
            }
            await ClearStateAsync();
            DeactivateOnIdle();
        }
    }
}