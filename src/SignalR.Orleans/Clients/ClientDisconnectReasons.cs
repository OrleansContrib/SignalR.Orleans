namespace SignalR.Orleans.Clients
{
    public static class ClientDisconnectReasons
    {
        public const string HubDisconnect = "hub-disconnect";
        public const string ServerDisconnected = "server-disconnected";
        public const string AttemptsLimitReached = "attempts-limit-reached";
    }
}