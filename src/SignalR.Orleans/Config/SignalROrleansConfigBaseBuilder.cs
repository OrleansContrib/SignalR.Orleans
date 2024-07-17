namespace SignalR.Orleans;

public class SignalROrleansConfigBaseBuilder
{
    public bool UseFireAndForgetDelivery { get; set; }

    public bool ConflateStorageAccess { get; set; } = false;
}

