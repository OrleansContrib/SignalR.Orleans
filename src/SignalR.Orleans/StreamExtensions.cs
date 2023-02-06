#nullable enable
using SignalR.Orleans.Core;

namespace SignalR.Orleans;

// todo: move to Stream Utils?
internal static class StreamReplicaExtensions
{
	public static string BuildReplicaStreamName(string streamName, int replicaIndex)
		=> $"{streamName}:{replicaIndex}";

	public static string BuildReplicaStreamNameRandom(string streamName, int replicas)
		=> BuildReplicaStreamName(streamName, Randomizer.Next(0, replicas));

	private static readonly Random Randomizer = new();

	/// <summary>
	/// Get stream replica either randomly according to the size given OR consistent based on the <paramref name="replicaId"/> (and partitioned based on its hash).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="streamProvider"></param>
	/// <param name="streamId"></param>
	/// <param name="streamNamespace"></param>
	/// <param name="replicas">Max replicas to obtain stream from.</param>
	/// <param name="replicaId">Consistent value to generate replica id for. e.g. primary key.</param>
	/// <returns></returns>
	public static IAsyncStream<T> GetStreamReplica<T>(
		this IStreamProvider streamProvider,
		Guid streamId,
		string streamNamespace,
		int replicas,
		string? replicaId = null
	)
	{
		var ns = string.IsNullOrEmpty(replicaId)
				? BuildReplicaStreamNameRandom(streamNamespace, replicas)
				: BuildReplicaStreamName(streamNamespace, replicaId.ToPartitionIndex(replicas));
		return streamProvider.GetStream<T>(streamId.ToString(), ns);
	}

	public static async Task ResumeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream, Func<T, StreamSequenceToken, Task> onNextAsync)
	{
		var subscriptions = await stream.GetAllSubscriptionHandles();
		if (subscriptions?.Count > 0)
		{
			var tasks = subscriptions.Select(x => x.ResumeAsync(onNextAsync));
			await Task.WhenAll(tasks);
		}
	}

	public static async Task UnsubscribeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream)
	{
		var subscriptions = await stream.GetAllSubscriptionHandles();
		if (subscriptions?.Count > 0)
		{
			var tasks = subscriptions.Select(x => x.UnsubscribeAsync());
			await Task.WhenAll(tasks);
		}
	}
}

/// <summary>
/// Contains streams replicated for sharding used for listening and managing multiple replicas easier.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal class StreamReplicaContainer<T>
{
	protected string DebuggerDisplay => $"StreamId: '{StreamId}', StreamNamespace: '{StreamNamespace}', MaxReplicas: {MaxReplicas}";

	public string StreamId { get; }
	public string StreamNamespace { get; }
	public int MaxReplicas { get; }

	private readonly List<IAsyncStream<T>> _streams = new();

	/// <summary>
	/// Create a new instance of stream with replicas.
	/// </summary>
	/// <param name="streamProvider"></param>
	/// <param name="streamId"></param>
	/// <param name="streamNamespace"></param>
	/// <param name="maxReplicas">Max replicas to create.</param>
	public StreamReplicaContainer(IStreamProvider streamProvider, string streamId, string streamNamespace, int maxReplicas)
	{
		StreamId = streamId;
		StreamNamespace = streamNamespace;
		MaxReplicas = maxReplicas;

		for (var i = 0; i < maxReplicas; i++)
		{
			var namespaceReplica = StreamReplicaExtensions.BuildReplicaStreamName(streamNamespace, i);
			_streams.Add(streamProvider.GetStream<T>(streamId, namespaceReplica));
		}
	}

	public StreamReplicaContainer(
		IStreamProvider streamProvider,
		Guid streamId,
		string streamNamespace,
		int maxReplicas
	) : this(streamProvider, streamId.ToString(), streamNamespace, maxReplicas)
	{
	}

	public async Task SubscribeAsync(Func<T, StreamSequenceToken, Task> onNextAsync)
	{
		var tasks = new List<Task>(MaxReplicas);

		foreach (var stream in _streams)
			tasks.Add(stream.SubscribeAsync(onNextAsync));

		await Task.WhenAll(tasks);
	}

	public async Task<IList<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles()
	{
		var tasks = new List<Task<IList<StreamSubscriptionHandle<T>>>>(MaxReplicas);

		foreach (var stream in _streams)
			tasks.Add(stream.GetAllSubscriptionHandles());

		var results = await Task.WhenAll(tasks);
		return results.SelectMany(x => x).ToList();
	}

	/// <summary>
	/// Publishes message in all replicas.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="token"></param>
	public async Task OnNextAsync(T item, StreamSequenceToken? token = null)
	{
		var tasks = new List<Task>(MaxReplicas);

		foreach (var stream in _streams)
			tasks.Add(stream.OnNextAsync(item, token));

		await Task.WhenAll(tasks);
	}
}