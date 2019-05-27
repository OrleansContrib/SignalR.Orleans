namespace SignalR.Orleans.Core
{
    internal static class Utils
    {
        internal static string BuildStreamHubName(string hubName) => $"registered-hub::{hubName}".ToLower();
    }
}