namespace SignalR.Orleans.Core
{
    internal static class Utils
    {
        internal static string BuildGrainId(string hubName, string key) => $"{hubName}:{key}";

        internal static string BuildStreamHubName(string hubName) => $"registered-hub::{hubName}".ToLower();

    }
}