using Microsoft.AspNetCore.SignalR;

namespace Silo;

public interface IChatHub
{
}

public class ChatHub : Hub, IChatHub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IClusterClient _clusterClient;

    public ChatHub(ILogger<ChatHub> logger, IClusterClient clusterClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
    }

    public async Task Send(string name, string message)
    {
        _logger.LogInformation($"{nameof(Send)} called. ConnectionId:{Context.ConnectionId}, Name:{name}, Message:{message}");

        var userNotificationGrain = _clusterClient.GetGrain<IUserNotificationGrain>(Guid.Empty.ToString());
        await userNotificationGrain.SendMessageAsync(name, message);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"{nameof(OnConnectedAsync)} called.");

        await base.OnConnectedAsync();
        await Groups.AddToGroupAsync(Context.ConnectionId, Guid.Empty.ToString());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(exception, $"{nameof(OnDisconnectedAsync)} called.");

        await base.OnDisconnectedAsync(exception);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Guid.Empty.ToString());
    }
}