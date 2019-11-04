using Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using SignalR.Orleans.Core;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class UserNotificationGrain : Grain, IUserNotificationGrain
    {
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
            var key = this.GetPrimaryKeyString();
            _logger.LogInformation($"{nameof(SendMessageAsync)} called. Name:{name}, Message:{message}, Key:{key}");

            var methodName = "broadcastMessage";
            _logger.LogInformation($"Sending message. MethodName:{methodName} Name:{name}, Message:{message}, Key:{key}");

            await _hubContext.Group(key).Send("BroadcastMessage", name, message);
        }
    }
}
