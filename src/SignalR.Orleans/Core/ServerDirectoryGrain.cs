﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
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
        public Dictionary<Guid, DateTime> Servers { get; set; } = new Dictionary<Guid, DateTime>();
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    public class ServerDirectoryGrain : Grain<ServerDirectoryState>, IServerDirectoryGrain
    {
        private IStreamProvider _streamProvider;

        private readonly ILogger<ServerDirectoryGrain> _logger;

        public ServerDirectoryGrain(ILogger<ServerDirectoryGrain> logger)
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            _streamProvider = GetStreamProvider(Constants.STREAM_PROVIDER);

            _logger.LogInformation("Available servers {serverIds}",
                string.Join(", ", State.Servers?.Count > 0 ? string.Join(", ", State.Servers) : "empty"));

            RegisterTimer(
               ValidateAndCleanUp,
               State,
               TimeSpan.FromSeconds(15),
               TimeSpan.FromMinutes(Constants.SERVERDIRECTORY_CLEANUP_IN_MINUTES));

            await base.OnActivateAsync();
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
                var serverDisconnectedStream = _streamProvider.GetStream<Guid>(server.Key, Constants.SERVER_DISCONNECTED);

                _logger.LogWarning("Removing server {serverId} due to inactivity {lastUpdatedDate}", server.Key, server.Value);
                await serverDisconnectedStream.OnNextAsync(server.Key);
                State.Servers.Remove(server.Key);
            }

            if (expiredServers.Count > 0)
                await WriteStateAsync();
        }
    }
}