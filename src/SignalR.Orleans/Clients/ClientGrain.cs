using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;
using SignalR.Orleans.Core;

namespace SignalR.Orleans.Clients;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal class ClientState
{
	private string DebuggerDisplay => $"ServerId: '{ServerId}'";

	public Guid ServerId { get; set; }
}

/// <summary>
/// Client (connection) which manage the connection to a server and dispatch messages to the hub manager e.g. Client -> HubManager
/// (via stream) to the connected server. e.g. '{hubName}:{connectionId}' ('hero:xyz123')
/// </summary>
[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
internal class ClientGrain : Grain<ClientState>, IClientGrain
{
	private readonly ILogger<ClientGrain> _logger;
	private IStreamProvider _streamProvider;
	private IAsyncStream<ClientMessage> _serverStream;
	private IAsyncStream<Guid> _serverDisconnectedStream;
	private ConnectionGrainKey _keyData;
	private const int MaxFailAttempts = 3;
	private int _failAttempts;

	public ClientGrain(ILogger<ClientGrain> logger)
	{
		_logger = logger;
	}

	public override async Task OnActivateAsync(CancellationToken cancellationToken)
	{
		_keyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
		_streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);

		if (State.ServerId == Guid.Empty)
			return;

		SetupStreams();

		await _serverDisconnectedStream.ResumeAllSubscriptionHandlers(
			async (serverId, _) => await OnDisconnect(ClientDisconnectReasons.ServerDisconnected)
		);
	}

	public async Task Send(Immutable<InvocationMessage> message)
	{
		if (State.ServerId != Guid.Empty)
		{
			_logger.LogDebug("Sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Value.Target, _keyData.Id);
			_failAttempts = 0;
			try
			{
				await _serverStream.OnNextAsync(new ClientMessage { ConnectionId = _keyData.Id, Payload = message.Value, HubName = _keyData.HubName });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error while sending message on {hubName}.{targetMethod} to connection {connectionId}", _keyData.HubName, message.Value.Target, _keyData.Id);
				throw;
			}
			return;
		}
		_failAttempts++;

		_logger.LogInformation("Client not connected for connectionId {connectionId} and hub {hubName} ({targetMethod}). FailedAttemptsCount: {failAttemptsCount}",
			_keyData.Id, _keyData.HubName, message.Value.Target, _failAttempts);

		if (_failAttempts >= MaxFailAttempts)
		{
			await OnDisconnect(ClientDisconnectReasons.AttemptsLimitReached);
			_logger.LogWarning("Force disconnect client for connectionId {connectionId} and hub {hubName} ({targetMethod}) after exceeding attempts limit. FailedAttemptsCount: {failAttemptsCount}",
				_keyData.Id, _keyData.HubName, message.Value.Target, _failAttempts);
		}
	}

	public Task SendOneWay(Immutable<InvocationMessage> message)
	{
		Send(message).Ignore();
		return Task.CompletedTask;
	}

	public async Task OnConnect(Guid serverId)
	{
		State.ServerId = serverId;
		SetupStreams();
		await _serverDisconnectedStream.SubscribeAsync(async (connId, _) => await OnDisconnect(ClientDisconnectReasons.ServerDisconnected));
		await WriteStateAsync();
	}

	public async Task OnDisconnect(string reason = null)
	{
		_logger.LogDebug("Disconnecting connection on {hubName} for connection {connectionId} from server {serverId} via {reason}",
			_keyData.HubName, _keyData.Id, State.ServerId, reason);

		if (_keyData.Id != null)
		{
			var clientDisconnectStream = _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, _keyData.Id);
			await clientDisconnectStream.OnNextAsync(_keyData.Id);
		}

		if (reason == ClientDisconnectReasons.HubDisconnect) // only cleanup if hub disconnects gracefully - otherwise don't so it can recover
			await ClearStateAsync();

		if (_serverDisconnectedStream != null)
			await _serverDisconnectedStream.UnsubscribeAllSubscriptionHandlers();
		DeactivateOnIdle();
	}

	private void SetupStreams()
	{
		_serverStream = _streamProvider.GetStreamReplica<ClientMessage>(
			State.ServerId,
			Constants.SERVERS_STREAM,
			Constants.STREAM_SEND_REPLICAS,
			this.GetPrimaryKeyString()
		);
		_serverDisconnectedStream = _streamProvider.GetStream<Guid>(StreamId.Create(Constants.SERVER_DISCONNECTED, State.ServerId));
	}
}