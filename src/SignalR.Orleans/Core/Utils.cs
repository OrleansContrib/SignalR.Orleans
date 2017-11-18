namespace SignalR.Orleans.Core
{
    public static class Utils
    {
        public static string BuildGrainName(string hubName, string key) => $"{hubName}:{key}".ToLower();
    }
}