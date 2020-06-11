// COPIED AND REFACTORED :: Microsoft.AspNetCore.SignalR.Tests

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Reflection;

namespace SignalR.Orleans.Tests.AspnetSignalR
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(ConnectionContext connection)
        {
            var ctx = new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance);

            var protocolProp = ctx.GetType().GetProperty(nameof(HubConnectionContext.Protocol), BindingFlags.Instance |
                                                                                                BindingFlags.NonPublic |
                                                                                                BindingFlags.Public);

            if (protocolProp != null) protocolProp.SetValue(ctx, new NewtonsoftJsonHubProtocol());

            return ctx;
        }
    }
}