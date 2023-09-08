using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace SignalR.Orleans.Core;

/// <summary>
/// A wrapper around the orleans persistent state component that batches operations in a reentrant-safe way to enable optimal throughput.
/// </summary>
internal class ConflatedPersistentState<TState> : IPersistentState<TState>
{
    public ConflatedPersistentState(IPersistentState<TState> state)
    {
        _state = state;
    }

    private readonly IPersistentState<TState> _state;
    private Task? _outstanding;

    public TState State
    {
        get => _state.State;
        set => _state.State = value;
    }

    public string Etag => _state.Etag;

    public bool RecordExists => _state.RecordExists;

    public Task ReadStateAsync() => ConflateStateAsync(OperationType.Read);

    public Task ClearStateAsync() => ConflateStateAsync(OperationType.Clear);

    public Task WriteStateAsync() => ConflateStateAsync(OperationType.Write);

    private async Task ConflateStateAsync(OperationType type)
    {
        var outstanding = _outstanding;
        if (outstanding != null)
        {
            try
            {
                await outstanding;
            }
            catch
            {
                // noop
            }
            finally
            {
                if (_outstanding == outstanding)
                {
                    _outstanding = null;
                }
            }
        }

        if (_outstanding == null)
        {
            outstanding = type switch
            {
                OperationType.Read => _state.ReadStateAsync(),
                OperationType.Write => _state.WriteStateAsync(),
                OperationType.Clear => _state.ClearStateAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            _outstanding = outstanding;
        }
        else
        {
            outstanding = _outstanding;
        }

        try
        {
            await outstanding;
        }
        finally
        {
            if (_outstanding == outstanding)
            {
                _outstanding = null;
            }
        }
    }

    private enum OperationType
    {
        Read,
        Write,
        Clear
    }
}

internal static class ConflatedPersistentStateExtensions
{
    public static IPersistentState<TState> WithConflation<TState>(this IPersistentState<TState> state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        return new ConflatedPersistentState<TState>(state);
    }
}