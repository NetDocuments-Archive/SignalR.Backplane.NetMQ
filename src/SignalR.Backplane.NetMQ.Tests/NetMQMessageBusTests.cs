namespace Signalr.Backplane.NetMQ
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using SampleServer;
    using Xunit;

    public class NetMQMessageBusTests
    {
        [Fact]
        public async Task Can_scale_out()
        {
            const int serverCount = 4;

            var allSubscriberPorts = Enumerable
                .Range(0, serverCount)
                .Select(i => 18100 + i)
                .ToArray();

            var testServers = Enumerable
                .Range(0, serverCount)
                .Select(i =>
                {
                    var httpPort = 8100 + i;
                    var netMQPort = 18100 + i;
                    var subscriberPorts = allSubscriberPorts.Except(new[] {netMQPort});
                    return new SignalRSampleServer(httpPort, netMQPort, subscriberPorts);
                })
                .ToArray();

            const string sentMessage = "Hello";

            var messagesReceived = Task.WhenAll(testServers
                .Skip(1)
                .Select(s =>
                {
                    var tcs = new TaskCompletionSource<string>();
                    s.Messages.Subscribe(tcs.SetResult);
                    return tcs.Task;
                })
                .ToArray());

            var stopwatch = Stopwatch.StartNew();
            await testServers[0].HubProxy.Invoke<string>("Send", sentMessage);

            var timeoutTask = Task.Delay(3000);

            if(await Task.WhenAny(messagesReceived, timeoutTask) == timeoutTask)
            {
                throw new TimeoutException("Timed out waiting for message");
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            foreach(var testServer in testServers)
            {
                testServer.Dispose();
            }
        }
    }
}