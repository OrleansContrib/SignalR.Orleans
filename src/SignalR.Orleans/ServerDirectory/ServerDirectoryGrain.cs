using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Timers;

namespace SignalR.Orleans.Core;

internal sealed class ServerDirectoryGrain : IGrainBase, IServerDirectoryGrain
{
    private const int SERVERDIRECTORY_CLEANUP_IN_MINUTES = SignalROrleansConstants.SERVER_HEARTBEAT_PULSE_IN_MINUTES * 3;

    private readonly ILogger<ServerDirectoryGrain> _logger;
    private readonly IPersistentState<ServerDirectoryState> _state;
    private readonly ITimerRegistry _timerRegistry;

    private IStreamProvider _streamProvider = default!;

    public IGrainContext GrainContext { get; }

    public ServerDirectoryGrain(
        ILogger<ServerDirectoryGrain> logger,
        IGrainContext grainContext,
        ITimerRegistry timerRegistry,
        [PersistentState(nameof(ServerDirectoryState), SignalROrleansConstants.SIGNALR_ORLEANS_STORAGE_PROVIDER)] IPersistentState<ServerDirectoryState> state)
    {
        this.GrainContext = grainContext;
        _timerRegistry = timerRegistry;
        _logger = logger;
        _state = state;
    }

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _streamProvider = this.GetOrleansSignalRStreamProvider();

        _logger.LogInformation("Available servers {serverIds}",
            string.Join(", ", _state.State.ServerHeartBeats?.Count > 0 ? string.Join(", ", _state.State.ServerHeartBeats) : "empty"));

        _timerRegistry.RegisterGrainTimer(this.GrainContext, ValidateAndCleanUp,
            _state.State, new GrainTimerCreationOptions(TimeSpan.FromSeconds(15), 
            TimeSpan.FromMinutes(SERVERDIRECTORY_CLEANUP_IN_MINUTES)));

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

    private async Task ValidateAndCleanUp(ServerDirectoryState serverDirectory, CancellationToken token)
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
