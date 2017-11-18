using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace SignalR.Orleans.Clients
{
    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class ClientGrain : Grain<ClientState>, IClientGrain
    {
        private IStreamProvider _streamProvider;
        private IAsyncStream<ClientMessage> _serverStream;
        private IAsyncStream<string> _clientDisconnectStream;

        public override Task OnActivateAsync()
        {
            this._streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);
            this._clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, Constants.CLIENT_DISCONNECT_STREAM);
            if (this.State.ServerId != Guid.Empty)
                this._serverStream = this._streamProvider.GetStream<ClientMessage>(this.State.ServerId, Constants.SERVERS_STREAM);
            return Task.CompletedTask;
        }

        public Task SendMessage(object message)
        {
            if (this.State.ServerId == Guid.Empty) throw new InvalidOperationException("Client not connected.");
            if (string.IsNullOrWhiteSpace(this.State.HubName)) throw new InvalidOperationException("Client hubname not set.");
            if (string.IsNullOrWhiteSpace(this.State.ConnectionId)) throw new InvalidOperationException("Client ConnectionId not set.");
            return this._serverStream.OnNextAsync(new ClientMessage { ConnectionId = State.ConnectionId, Payload = message, HubName = State.HubName });
        }

        public Task OnConnect(Guid serverId, string hubName, string connectionId)
        {
            this.State.ServerId = serverId;
            this.State.HubName = hubName;
            this.State.ConnectionId = connectionId;
            this._serverStream = this._streamProvider.GetStream<ClientMessage>(this.State.ServerId, Constants.SERVERS_STREAM);
            return this.WriteStateAsync();
        }

        public async Task OnDisconnect()
        {
            await this._clientDisconnectStream.OnNextAsync(this.State.ConnectionId);
            await this.ClearStateAsync();
            this.DeactivateOnIdle();
        }
    }

    internal class ClientState
    {
        public Guid ServerId { get; set; }
        public string ConnectionId { get; set; }
        public string HubName { get; set; }
    }
}