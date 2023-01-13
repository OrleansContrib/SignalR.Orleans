﻿using Microsoft.AspNetCore.SignalR.Protocol;
using Orleans.Concurrency;

namespace SignalR.Orleans
{
    [Immutable]
    public sealed record AllMessage(Immutable<InvocationMessage> Message, IReadOnlyList<string>? ExcludedIds = null);
}