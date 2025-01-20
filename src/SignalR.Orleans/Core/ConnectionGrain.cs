using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Runtime;

namespace SignalR.Orleans.Core;

// todo: rename to ConnectionGroupGrain
/// <summary>
/// Connection grain is responsible for grouping connections by hub name and group e.g. '{hubName}:{groupName}' ('hero:top').
/// This will be used to send messages to all connections in the group e.g. Group -> Client -> HubManager
/// </summary>
/// <typeparam name="TGrainState"></typeparam>
internal abstract class ConnectionGrain<TGrainState> : Grain<TGrainState>, IConnectionGrain
	where TGrainState : ConnectionState, new()
{
	private readonly ILogger _logger;
	private IStreamProvider _streamProvider;
	private readonly HashSet<string> _connectionStreamToUnsubscribe = new();
	private readonly TimeSpan _cleanupPeriod = TimeSpan.Parse(Constants.CONNECTION_STREAM_CLEANUP);

	protected ConnectionGrainKey KeyData;
	private IDisposable _cleanupTimer;

	internal ConnectionGrain(ILogger logger)
	{
		_logger = logger;
	}

	public override async Task OnActivateAsync(CancellationToken cancellationToken)
	{
		KeyData = new ConnectionGrainKey(this.GetPrimaryKeyString());
		_logger.LogInformation("Activate {hubName} ({groupId})", KeyData.HubName, KeyData.Id);
		_streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);

		_cleanupTimer = RegisterTimer(
			_ => CleanupStreams(),
			State,
			_cleanupPeriod,
			_cleanupPeriod);

		if (State.Connections.Count == 0)
		{
			return;
		}

		foreach (var connection in State.Connections)
		{
			var clientDisconnectStream = GetClientDisconnectStream(connection);
			await clientDisconnectStream.ResumeAllSubscriptionHandlers(async (connectionId, _) => await Remove(connectionId));
		}
	}

	public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deactivate {hubName} ({groupId})", KeyData.HubName, KeyData.Id);
		_cleanupTimer?.Dispose();
		return CleanupStreams();
	}

	public virtual async Task Add(string connectionId)
	{
		if (!State.Connections.Add(connectionId))
			return;
		_logger.LogInformation("Added connection '{connectionId}' on {hubName} ({groupId}). {connectionsCount} connection(s)",
			connectionId, KeyData.HubName, KeyData.Id, State.Connections.Count);

		var clientDisconnectStream = GetClientDisconnectStream(connectionId);
		await clientDisconnectStream.SubscribeAsync(async (connId, _) => await Remove(connId));
		await WriteStateAsync();
	}

	public virtual async Task Remove(string connectionId)
	{
		var shouldWriteState = State.Connections.Remove(connectionId);
		_logger.LogInformation("Removing connection '{connectionId}' on {hubName} ({groupId}). Remaining {connectionsCount} connection(s), was found: {isConnectionFound}",
			connectionId, KeyData.HubName, KeyData.Id, State.Connections.Count, shouldWriteState);
		_connectionStreamToUnsubscribe.Add(connectionId);

		if (State.Connections.Count == 0)
		{
			await ClearStateAsync();
		}
		else if (shouldWriteState)
		{
			await WriteStateAsync();
		}
	}

	public virtual Task Send(Immutable<InvocationMessage> message)
		=> SendAll(message, State.Connections);

	public Task SendOneWay(Immutable<InvocationMessage> message)
	{
		Send(message).Ignore();
		return Task.CompletedTask;
	}

	public Task SendExcept(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
	{
		var message = new Immutable<InvocationMessage>(new InvocationMessage(methodName, args));
		return SendAll(message, State.Connections.Where(x => !excludedConnectionIds.Contains(x)).ToList());
	}

	public Task<int> Count()
		=> Task.FromResult(State.Connections.Count);

	protected Task SendAll(Immutable<InvocationMessage> message, IReadOnlyCollection<string> connections)
	{
		_logger.LogDebug("Sending message to {hubName}.{targetMethod} on group {groupId} to {connectionsCount} connection(s)",
			KeyData.HubName, message.Value.Target, KeyData.Id, connections.Count);

		foreach (var connection in connections)
			GrainFactory.GetClientGrain(KeyData.HubName, connection).SendOneWay(message);

		return Task.CompletedTask;
	}

	private async Task CleanupStreams()
	{
		if (_connectionStreamToUnsubscribe.Count > 0)
		{
			foreach (var connectionId in _connectionStreamToUnsubscribe.ToList())
			{
				await GetClientDisconnectStream(connectionId).UnsubscribeAllSubscriptionHandlers();
				_connectionStreamToUnsubscribe.Remove(connectionId);
			}
		}
	}

	private IAsyncStream<string> GetClientDisconnectStream(string connectionId)
		=> _streamProvider.GetStream<string>(Constants.CLIENT_DISCONNECT_STREAM_ID, connectionId);
}

[GenerateSerializer]
internal abstract class ConnectionState
{
	[Id(0)]
	public HashSet<string> Connections { get; set; } = new HashSet<string>();
}