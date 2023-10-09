namespace SignalR.Orleans;

public static class Constants
{
	public const string PUBSUB_PROVIDER = "PubSubStore";
	// todo: ideally it doesnt use the default name so consumers can replace the provider and not affecting the default - it will be breaking tho.
	//public const string PUBSUB_PROVIDER = "ORLEANS_SIGNALR_PUBSUB_PROVIDER";

	public const string STORAGE_PROVIDER = "ORLEANS_SIGNALR_STORAGE_PROVIDER";

	public const string SERVERS_STREAM = "SERVERS_STREAM";
	public const string SERVER_DISCONNECTED = "SERVER_DISCONNECTED";
	public const string STREAM_PROVIDER = "ORLEANS_SIGNALR_STREAM_PROVIDER";
	public static readonly string CLIENT_DISCONNECT_STREAM_ID = "CLIENT_DISCONNECT_STREAM";
	public static readonly string ALL_STREAM_ID = "ALL_STREAM";

	internal const int STREAM_SEND_REPLICAS = 10; // todo: make configurable instead
	internal const double HEARTBEAT_PULSE_IN_MINUTES = 30;
	internal const double SERVERDIRECTORY_CLEANUP_IN_MINUTES = HEARTBEAT_PULSE_IN_MINUTES * 3;
	internal const string CONNECTION_STREAM_CLEANUP = "0:01:00";
}