namespace Signalr.Backplane.NetMQ
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using SampleClient;
    using SampleServer;
    using Xunit;

    public class NetMQMessageBusTests
    {
        [Fact]
        public async Task Can_scale_out()
        {
            var servers = new[]
            {
                new SampleSignalRServer(8100, 18100, new[] {18101, 18102}),
                new SampleSignalRServer(8101, 18101, new[] {18100, 18102}),
                new SampleSignalRServer(8102, 18102, new[] {18100, 18101})
            };

            var cients = new[]
            {
                new SampleSignalRClient(8100),
                new SampleSignalRClient(8101),
                new SampleSignalRClient(8102)
            };

            const string message = "Hello";

            var messagesReceived = Task.WhenAll(cients
                .Skip(1)
                .Select(s =>
                {
                    var tcs = new TaskCompletionSource<string>();
                    s.Messages.Subscribe(tcs.SetResult);
                    return tcs.Task;
                })
                .ToArray());

            var stopwatch = Stopwatch.StartNew();
            await cients[0].Send(message);

            var timeoutTask = Task.Delay(3000);

            if(await Task.WhenAny(messagesReceived, timeoutTask) == timeoutTask)
            {
                throw new TimeoutException("Timed out waiting for message");
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds);

            foreach (var server in servers)
            {
                server.Dispose();
            }
        }
    }
}