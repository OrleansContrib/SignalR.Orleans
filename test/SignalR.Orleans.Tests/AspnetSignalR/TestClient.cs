// COPIED AND REFACTORED :: Microsoft.AspNetCore.SignalR.Tests

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace SignalR.Orleans.Tests.AspnetSignalR
{
    public class TestClient : ITransferFormatFeature, IConnectionHeartbeatFeature, IDisposable
    {
        private static int _id;
        private readonly IHubProtocol _protocol;
        private readonly IInvocationBinder _invocationBinder;
        private readonly CancellationTokenSource _cts;

        public DefaultConnectionContext Connection { get; }
        public Task Connected => ((TaskCompletionSource<bool>)Connection.Items["ConnectedTask"]).Task;

        public TestClient(bool synchronousCallbacks = false, IHubProtocol protocol = null, IInvocationBinder invocationBinder = null, bool addClaimId = false)
        {
            var pair = DuplexPipePair.GetConnectionTransport(synchronousCallbacks);
            Connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Transport, pair.Application);

            // Add features SignalR needs for testing
            Connection.Features.Set<ITransferFormatFeature>(this);
            Connection.Features.Set<IConnectionHeartbeatFeature>(this);

            var claimValue = Interlocked.Increment(ref _id).ToString();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, claimValue) };
            if (addClaimId)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
            }

            Connection.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            Connection.Items["ConnectedTask"] = new TaskCompletionSource<bool>();

            _protocol = protocol ?? new JsonHubProtocol();
            _invocationBinder = invocationBinder ?? new DefaultInvocationBinder();

            _cts = new CancellationTokenSource();
        }

        public async Task<IList<HubMessage>> StreamAsync(string methodName, params object[] args)
        {
            var invocationId = await SendStreamInvocationAsync(methodName, args);

            var messages = new List<HubMessage>();
            while (true)
            {
                var message = await ReadAsync();

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
                }

                if (message is HubInvocationMessage hubInvocationMessage && !string.Equals(hubInvocationMessage.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage _:
                        messages.Add(message);
                        break;
                    case CompletionMessage _:
                        messages.Add(message);
                        return messages;
                    default:
                        throw new NotSupportedException("TestClient does not support receiving invocations!");
                }
            }
        }

        public async Task<CompletionMessage> InvokeAsync(string methodName, params object[] args)
        {
            var invocationId = await SendInvocationAsync(methodName, nonBlocking: false, args: args);

            while (true)
            {
                var message = await ReadAsync();

                if (message == null)
                {
                    throw new InvalidOperationException("Connection aborted!");
                }

                if (message is HubInvocationMessage hubInvocationMessage && !string.Equals(hubInvocationMessage.InvocationId, invocationId))
                {
                    throw new NotSupportedException("TestClient does not support multiple outgoing invocations!");
                }

                switch (message)
                {
                    case StreamItemMessage result:
                        throw new NotSupportedException("Use 'StreamAsync' to call a streaming method");
                    case CompletionMessage completion:
                        return completion;
                    case PingMessage _:
                        // Pings are ignored
                        break;
                    default:
                        throw new NotSupportedException("TestClient does not support receiving invocations!");
                }
            }
        }

        public Task<string> SendInvocationAsync(string methodName, params object[] args)
        {
            return SendInvocationAsync(methodName, nonBlocking: false, args: args);
        }

        public Task<string> SendInvocationAsync(string methodName, bool nonBlocking, params object[] args)
        {
            var invocationId = nonBlocking ? null : GetInvocationId();
            return SendHubMessageAsync(new InvocationMessage(invocationId, methodName,
                argumentBindingException: null, arguments: args));
        }

        public Task<string> SendStreamInvocationAsync(string methodName, params object[] args)
        {
            var invocationId = GetInvocationId();
            return SendHubMessageAsync(new StreamInvocationMessage(invocationId, methodName,
                argumentBindingException: null, arguments: args));
        }


        public async Task<string> SendHubMessageAsync(HubMessage message)
        {
            var payload = _protocol.WriteToArray(message);

            await Connection.Application.Output.WriteAsync(payload);
            return message is HubInvocationMessage hubMessage ? hubMessage.InvocationId : null;
        }

        public async Task<HubMessage> ReadAsync(bool isHandshake = false)
        {
            while (true)
            {
                var message = TryRead(isHandshake);

                if (message == null)
                {
                    var result = await Connection.Application.Input.ReadAsync().OrTimeout();
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            continue;
                        }

                        if (result.IsCompleted)
                        {
                            return null;
                        }
                    }
                    finally
                    {
                        Connection.Application.Input.AdvanceTo(buffer.Start);
                    }
                }
                else
                {
                    return message;
                }
            }
        }

        public HubMessage TryRead(bool isHandshake = false)
        {
            if (!Connection.Application.Input.TryRead(out var result))
            {
                return null;
            }

            var buffer = result.Buffer;

            try
            {
                if (!isHandshake)
                {
                    if (_protocol.TryParseMessage(ref buffer, _invocationBinder, out var message))
                    {
                        return message;
                    }
                }
                else
                {
                    // read first message out of the incoming data 
                    if (HandshakeProtocol.TryParseResponseMessage(ref buffer, out var responseMessage))
                    {
                        return responseMessage;
                    }
                }
            }
            finally
            {
                Connection.Application.Input.AdvanceTo(buffer.Start);
            }

            return null;
        }

        public void Dispose()
        {
            _cts.Cancel();

            Connection.Application.Output.Complete();
        }

        private static string GetInvocationId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private class DefaultInvocationBinder : IInvocationBinder
        {
            public IReadOnlyList<Type> GetParameterTypes(string methodName)
            {
                // TODO: Possibly support actual client methods
                return new[] { typeof(object) };
            }

            public Type GetReturnType(string invocationId)
            {
                return typeof(object);
            }
        }

        public TransferFormat SupportedFormats { get; set; } = TransferFormat.Text | TransferFormat.Binary;
        public TransferFormat ActiveFormat { get; set; }
        private readonly object _heartbeatLock = new object();
        private List<(Action<object> handler, object state)> _heartbeatHandlers;


        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }
                _heartbeatHandlers.Add((action, state));
            }
        }
    }
}
