// COPIED AND REFACTORED :: Microsoft.AspNetCore.SignalR.Tests
// TODO: Since we're up a couple of SignalR versions now, this could have changed -- should revisit original implementation

using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace SignalR.Orleans.Tests.AspnetSignalR
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(ConnectionContext connection)
        {
            var ctx = new HubConnectionContext(connection, new HubConnectionContextOptions() { ClientTimeoutInterval = TimeSpan.FromSeconds(15) }, NullLoggerFactory.Instance);
            var protocolProp = ctx.GetType().GetProperty("Protocol", BindingFlags.Instance |
                                                                     BindingFlags.NonPublic |
                                                                     BindingFlags.Public)!;
            protocolProp.SetValue(ctx, new JsonHubProtocol());
            return ctx;
        }
    }
}