using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans.Clients;

[Immutable, GenerateSerializer]
public class ClientMessage
{
	[Id(0)]
	public string HubName { get; set; }
	[Id(1)]
	public string ConnectionId { get; set; }
	[Id(2), Immutable]
	public InvocationMessage Payload { get; set; }
}