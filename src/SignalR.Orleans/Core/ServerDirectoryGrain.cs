using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    public interface IServerDirectoryGrain : IGrainWithIntegerKey
    {
        Task Heartbeat(Guid serverId);
        Task Unregister(Guid serverId);
    }

    public class ServerDirectoryState
    {
        public Dictionary<Guid, DateTime> Servers { get; set; } = new();
    }

    public class ServerDirectoryGrain : Grain, IServerDirectoryGrain
    {
        private readonly ILogger<ServerDirectoryGrain> _logger;
        private readonly IPersistentState<ServerDirectoryState> _directory;
        private IStreamProvider _streamProvider = default!;

        public ServerDirectoryGrain(
            ILogger<ServerDirectoryGrain> logger,
            [PersistentState(Constants.STORAGE_PROVIDER)] IPersistentState<ServerDirectoryState> directoryState)
        {
            _logger = logger;
            _directory = directoryState;
        }

        public override Task OnActivateAsync()
        {
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);

            _logger.LogInformation("Available servers {serverIds}",
                string.Join(", ", _directory.State.Servers?.Count > 0 ? string.Join(", ", _directory.State.Servers) : "empty"));

            RegisterTimer(
               ValidateAndCleanUp,
               _directory.State,
               TimeSpan.FromSeconds(15),
               TimeSpan.FromMinutes(Constants.SERVERDIRECTORY_CLEANUP_IN_MINUTES));

            return Task.CompletedTask;
        }

        public Task Heartbeat(Guid serverId)
        {
            _directory.State.Servers[serverId] = DateTime.UtcNow;
            return _directory.WriteStateAsync();
        }

        public async Task Unregister(Guid serverId)
        {
            if (!_directory.State.Servers.ContainsKey(serverId))
                return;

            _logger.LogWarning("Unregister server {serverId}", serverId);
            _directory.State.Servers.Remove(serverId);
            await _directory.WriteStateAsync();
        }

        private async Task ValidateAndCleanUp(object serverDirectory)
        {
            var expiredServers = _directory.State.Servers.Where(server => server.Value < DateTime.UtcNow.AddMinutes(-Constants.SERVERDIRECTORY_CLEANUP_IN_MINUTES)).ToList();
            foreach (var server in expiredServers)
            {
                var serverDisconnectedStream = _streamProvider.GetStream<Guid>(server.Key, Constants.SERVER_DISCONNECTED);

                _logger.LogWarning("Removing server {serverId} due to inactivity {lastUpdatedDate}", server.Key, server.Value);
                await serverDisconnectedStream.OnNextAsync(server.Key);
                _directory.State.Servers.Remove(server.Key);
            }

            if (expiredServers.Count > 0)
                await _directory.WriteStateAsync();
        }
    }
}