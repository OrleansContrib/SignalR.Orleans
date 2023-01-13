// COPIED AND REFACTORED :: Signalr/src/common/DuplexPipe.cs
// TODO: Since we're up a couple of SignalR versions now, this could have changed -- should revisit original implementation

namespace System.IO.Pipelines
{
    public class DuplexPipe : IDuplexPipe
    {
        public DuplexPipe(PipeReader reader, PipeWriter writer)
        {
            Input = reader;
            Output = writer;
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }
    }

    public class DuplexPipePair
    {
        public IDuplexPipe Transport { get; }
        public IDuplexPipe Application { get; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }

        public static DuplexPipePair GetConnectionTransport(bool synchronousCallbacks)
        {
            var scheduler = synchronousCallbacks ? PipeScheduler.Inline : null;
            var options = new PipeOptions(readerScheduler: scheduler, writerScheduler: scheduler, useSynchronizationContext: false);
            var input = new Pipe(options);
            var output = new Pipe(options);

            var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
            var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

            return new DuplexPipePair(applicationToTransport, transportToApplication);
        }
    }
}