using System;
using System.Threading.Tasks;
using Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using SignalR.Orleans.Core;

namespace Grains
{
    public class UserNotificationGrain : Grain, IUserNotificationGrain
    {
        private const string BroadcastMessage = "BroadcastMessage";
        private readonly ILogger<UserNotificationGrain> _logger;
        private HubContext<IChatHub> _hubContext;

        public UserNotificationGrain(ILogger<UserNotificationGrain> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task OnActivateAsync()
        {
            _logger.LogInformation($"{nameof(OnActivateAsync)} called");
            _hubContext = GrainFactory.GetHub<IChatHub>();
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
}
