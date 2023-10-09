﻿namespace SignalR.Orleans.Core;

public interface IServerDirectoryGrain : IGrainWithIntegerKey
{
	Task Heartbeat(Guid serverId);
	Task Unregister(Guid serverId);
}

[GenerateSerializer]
public class ServerDirectoryState
{
	[Id(0)]
	public Dictionary<Guid, DateTime> Servers { get; set; } = new();
}

[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
public class ServerDirectoryGrain : Grain<ServerDirectoryState>, IServerDirectoryGrain
{
	private IStreamProvider _streamProvider;

	private readonly ILogger<ServerDirectoryGrain> _logger;
	private IDisposable _cleanupTimer;

	public ServerDirectoryGrain(ILogger<ServerDirectoryGrain> logger)
	{
		_logger = logger;
	}

	public override Task OnActivateAsync(CancellationToken cancellationToken)
	{
		_streamProvider = this.GetStreamProvider(Constants.STREAM_PROVIDER);

		_logger.LogInformation("Available servers {serverIds}",
			string.Join(", ", State.Servers?.Count > 0 ? string.Join(", ", State.Servers) : "empty"));

		_cleanupTimer = RegisterTimer(
			ValidateAndCleanUp,
			State,
			TimeSpan.FromSeconds(15),
			TimeSpan.FromMinutes(Constants.SERVERDIRECTORY_CLEANUP_IN_MINUTES));

		return Task.CompletedTask;
	}

	public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
	{
		_cleanupTimer?.Dispose();
		return Task.CompletedTask;
	}

	public Task Heartbeat(Guid serverId)
	{
		State.Servers[serverId] = DateTime.UtcNow;
		return WriteStateAsync();
	}

	public async Task Unregister(Guid serverId)
	{
		if (!State.Servers.ContainsKey(serverId))
			return;

		_logger.LogWarning("Unregister server {serverId}", serverId);
		State.Servers.Remove(serverId);
		await WriteStateAsync();
	}

	private async Task ValidateAndCleanUp(object serverDirectory)
	{
		var expiredServers = State.Servers.Where(server => server.Value < DateTime.UtcNow.AddMinutes(-Constants.SERVERDIRECTORY_CLEANUP_IN_MINUTES)).ToList();
		foreach (var server in expiredServers)
		{
			var serverDisconnectedStream = _streamProvider.GetStream<Guid>(Constants.SERVER_DISCONNECTED, server.Key);

			_logger.LogWarning("Removing server {serverId} due to inactivity {lastUpdatedDate}", server.Key, server.Value);
			await serverDisconnectedStream.OnNextAsync(server.Key);
			State.Servers.Remove(server.Key);
		}

		if (expiredServers.Count > 0)
			await WriteStateAsync();
	}
}