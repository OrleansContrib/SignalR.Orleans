using Orleans.Runtime;
using SignalR.Orleans.Core;

namespace Silo;

public class UserNotificationGrain : IGrainBase, IUserNotificationGrain
{
    private const string BroadcastMessage = "BroadcastMessage";
    private readonly ILogger<UserNotificationGrain> _logger;
    private readonly IGrainFactory _grainFactory;
    private HubContext<ChatHub> _hubContext = default!;

    public UserNotificationGrain(
        ILogger<UserNotificationGrain> logger, 
        IGrainContext context,
        IGrainFactory grainFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        GrainContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IGrainContext GrainContext { get; }

    public Task OnActivateAsync(CancellationToken ct)
    {
        _logger.LogInformation($"{nameof(OnActivateAsync)} called");
        _hubContext = this._grainFactory.GetHub<ChatHub>();
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(string name, string message)
    {
        var groupId = this.GetPrimaryKeyString();
        _logger.LogInformation($"{nameof(SendMessageAsync)} called. Name:{name}, Message:{message}, Key:{groupId}");
        _logger.LogInformation($"Sending message to group: {groupId}. MethodName:{BroadcastMessage} Name:{name}, Message:{message}");

        await _hubContext.Group(groupId).Send(BroadcastMessage, name, message);
    }
}