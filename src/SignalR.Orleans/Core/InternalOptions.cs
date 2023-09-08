namespace SignalR.Orleans.Core;

/// <summary>
/// This internal options class is here to avoid breaking the existing signature of UseSignalR()
/// </summary>
internal class InternalOptions
{
    public bool ConflateStorageAccess { get; set; } = false;
}