// COPIED AND REFACTORED :: Microsoft.AspNetCore.SignalR.Tests

using System;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace SignalR.Orleans.Tests.AspnetSignalR
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(ConnectionContext connection)
        {
            var ctx = new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance);
            var protocolProp = ctx.GetType().GetProperty("Protocol", BindingFlags.Instance |
                                                                     BindingFlags.NonPublic |
                                                                     BindingFlags.Public);
            protocolProp.SetValue(ctx, new JsonHubProtocol());
            return ctx;
        }
    }
}