namespace SignalR.Orleans;

public class SignalRClientConfig
{
    public bool UseFireAndForgetDelivery { get; set; }

    public bool ConflateStorageAccess { get; set; } = false;
}

