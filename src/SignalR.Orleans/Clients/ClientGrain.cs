using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

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
        private string _connectionId;
        private string _hubName;

        public override Task OnActivateAsync()
        {
            ParsePrimaryKey();

            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);
            _clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, _connectionId);

            if (State.ServerId == Guid.Empty)
                return Task.CompletedTask;

            _serverStream = _streamProvider.GetStream<ClientMessage>(State.ServerId, Constants.SERVERS_STREAM);
            return Task.CompletedTask;
        }

        public Task SendMessage(object message)
        {
            if (State.ServerId == Guid.Empty) throw new InvalidOperationException("Client not connected.");
            return _serverStream.OnNextAsync(new ClientMessage { ConnectionId = _connectionId, Payload = message, HubName = _hubName });
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
            if (_connectionId != null)
            {
                await _clientDisconnectStream.OnNextAsync(_connectionId);
            }
            await ClearStateAsync();
            DeactivateOnIdle();
        }

        private void ParsePrimaryKey()
        {
            var pk = this.GetPrimaryKeyString();
            var pkArray = pk.Split(':');
            _hubName = pkArray[0];
            _connectionId = pkArray[1];
        }
    }
}