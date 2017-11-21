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
            this._clientDisconnectStream = this._streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, this.GetPrimaryKeyString());
            if (this.State.ServerId != Guid.Empty)
                this._serverStream = this._streamProvider.GetStream<ClientMessage>(this.State.ServerId, Constants.SERVERS_STREAM);
            return Task.CompletedTask;
        }

        public Task SendMessage(object message)
        {
            if (this.State.ServerId == Guid.Empty) throw new InvalidOperationException("Client not connected.");
            return this._serverStream.OnNextAsync(new ClientMessage { ConnectionId = this.GetPrimaryKeyString(), Payload = message });
        }

        public Task OnConnect(Guid serverId)
        {
            this.State.ServerId = serverId;
            this._serverStream = this._streamProvider.GetStream<ClientMessage>(this.State.ServerId, Constants.SERVERS_STREAM);
            return this.WriteStateAsync();
        }

        public async Task OnDisconnect()
        {
            await this.ClearStateAsync();
            await this._clientDisconnectStream.OnNextAsync(this.GetPrimaryKeyString());
            this.DeactivateOnIdle();
        }
    }

    internal class ClientState
    {
        public Guid ServerId { get; set; }
    }
}