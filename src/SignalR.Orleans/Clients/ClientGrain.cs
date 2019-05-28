using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
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
        private IStreamProvider _streamProvider;
        private IAsyncStream<ClientMessage> _serverStream;
        private IAsyncStream<string> _clientDisconnectStream;
        private ConnectionGrainKey _keyData;

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

        public Task Send(InvocationMessage message)
        {
            if (State.ServerId == Guid.Empty) throw new InvalidOperationException("Client not connected.");
            return _serverStream.OnNextAsync(new ClientMessage { ConnectionId = _keyData.Id, Payload = message, HubName = _keyData.HubName });
        }

        public Task OnConnect(Guid serverId)
        {
            // todo: can this connect if its already connected?
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