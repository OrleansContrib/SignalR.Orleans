namespace SignalR.Orleans.Core
{
    public interface IServerDirectoryGrain : IGrainWithIntegerKey
    {
        Task Heartbeat(Guid serverId);
        Task Unregister(Guid serverId);
    }
}