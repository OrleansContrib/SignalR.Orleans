using Microsoft.AspNetCore.SignalR.Protocol;

namespace SignalR.Orleans;

[GenerateSerializer]
public class AllMessage
{
	[Id(0)]
	public IReadOnlyList<string> ExcludedIds { get; set; }
	[Id(1), Immutable]
	public InvocationMessage Payload { get; set; }
}

[GenerateSerializer, Immutable]
public readonly struct InvocationMessageSurrogate
{
    [Id(0)]
    public readonly string InvocationId;

    [Id(1)]
    public readonly string Target;

    [Id(2)]
    public readonly object[] Arguments;

    [Id(3)]
    public readonly string[] StreamIds;

    [Id(4)]
    public readonly IDictionary<string, string> Headers;

    public InvocationMessageSurrogate(string invocationId, string target, object[] arguments, string[] streamIds, IDictionary<string, string> headers)
    {
        InvocationId = invocationId;
        Target = target;
        Arguments = arguments;
        StreamIds = streamIds;
        Headers = headers;
    }
}

[RegisterConverter]
public sealed class InvocationMessageSurrogateConverter : IConverter<InvocationMessage, InvocationMessageSurrogate>
{
    public InvocationMessage ConvertFromSurrogate(in InvocationMessageSurrogate surrogate) =>
	    new(
		    invocationId: surrogate.InvocationId,
		    target: surrogate.Target,
		    arguments: surrogate.Arguments,
		    streamIds: surrogate.StreamIds)
	    {
		    Headers = surrogate.Headers,
	    };

    public InvocationMessageSurrogate ConvertToSurrogate(in InvocationMessage value) =>
	    new(
		    invocationId: value.InvocationId,
		    target: value.Target,
		    arguments: value.Arguments,
		    streamIds: value.StreamIds,
		    headers: value.Headers);
}
