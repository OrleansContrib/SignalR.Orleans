using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace SignalR.Orleans.Core
{
    internal sealed class ServerDirectoryGrain : Grain, IServerDirectoryGrain
    {
        private const int SERVERDIRECTORY_CLEANUP_IN_MINUTES = Constants.SERVER_HEARTBEAT_PULSE_IN_MINUTES * 3;

        private readonly ILogger<ServerDirectoryGrain> _logger;
        private readonly IPersistentState<ServerDirectoryState> _state;

        private IStreamProvider _streamProvider = default!;

        public ServerDirectoryGrain(
            ILogger<ServerDirectoryGrain> logger,
            [PersistentState(nameof(ServerDirectoryState), Constants.STORAGE_PROVIDER)] IPersistentState<ServerDirectoryState> state)
        {
            _logger = logger;
            _state = state;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _streamProvider = this.GetOrleansSignalRStreamProvider();

            _logger.LogInformation("Available servers {serverIds}",
                string.Join(", ", _state.State.ServerHeartBeats?.Count > 0 ? string.Join(", ", _state.State.ServerHeartBeats) : "empty"));

            RegisterTimer(
               ValidateAndCleanUp,
               _state.State,
               TimeSpan.FromSeconds(15),
               TimeSpan.FromMinutes(SERVERDIRECTORY_CLEANUP_IN_MINUTES));

            return Task.CompletedTask;
        }

        public async Task Heartbeat(Guid serverId)
        {
            _state.State.ServerHeartBeats[serverId] = DateTime.UtcNow;
            await _state.WriteStateAsync();
        }

        public async Task Unregister(Guid serverId)
        {
            if (!_state.State.ServerHeartBeats.ContainsKey(serverId))
                return;

            _logger.LogInformation("Unregister server {serverId}", serverId);
            _state.State.ServerHeartBeats.Remove(serverId);
            await _state.WriteStateAsync();
        }

        private async Task ValidateAndCleanUp(object serverDirectory)
        {
            var inactiveTime = DateTime.UtcNow.AddMinutes(-SERVERDIRECTORY_CLEANUP_IN_MINUTES);
            var expiredHeartBeats = _state.State.ServerHeartBeats.Where(heartBeat => heartBeat.Value < inactiveTime).ToList();

            if (expiredHeartBeats.Count > 0)
            {
                foreach (var heartBeat in expiredHeartBeats)
                {
                    var serverId = heartBeat.Key;
                    _logger.LogWarning("Removing server {serverId} due to inactivity {lastUpdatedDate}", serverId, heartBeat.Value);
                    await _streamProvider.GetServerDisconnectionStream(serverId).OnNextAsync(serverId);
                    _state.State.ServerHeartBeats.Remove(serverId);
                }

                await _state.WriteStateAsync();
            }
        }
    }
}