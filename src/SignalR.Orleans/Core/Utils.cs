namespace SignalR.Orleans.Core
{
    internal static class Utils
    {
        internal static string BuildGrainName(string hubName, string key) => $"{hubName}:{key}".ToLower();
    }
}