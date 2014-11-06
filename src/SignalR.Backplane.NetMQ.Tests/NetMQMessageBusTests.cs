namespace Signalr.Backplane.NetMQ
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Client;
    using Microsoft.Owin.Hosting;
    using Owin;
    using SignalR.Backplane.NetMQ.Annotations;
    using Xunit;

    public class NetMQMessageBusTests
    {
        [Fact]
        public async Task Can_scale_out()
        {
            const string serverUrl1 = "http://localhost:8180";
            const string serverUrl2 = "http://localhost:8181";
            var config1 = new NetMQScaleoutConfiguration("tcp://127.0.0.1:18180", new[] { "tcp://127.0.0.1:18181" });
            var config2 = new NetMQScaleoutConfiguration("tcp://127.0.0.1:18181", new[] { "tcp://127.0.0.1:18180" });

            Action<IAppBuilder, NetMQScaleoutConfiguration> appBuilder = (app, config) =>
            {
                var resolver = new DefaultDependencyResolver();
                resolver.UseNetMQServiceBus(config);
                var hubConfiguration = new HubConfiguration
                {
                    Resolver = resolver
                };
                app.MapSignalR(hubConfiguration);
            };

            using (WebApp.Start(serverUrl1, app => appBuilder(app, config1)))
            {
                using (WebApp.Start(serverUrl2, app => appBuilder(app, config2)))
                {
                    var tcs = new TaskCompletionSource<string>();
                    const string hubName = "ChatHub";
                    var hubConnection1 = new HubConnection(serverUrl1);
                    IHubProxy chatHubProxy1 = hubConnection1.CreateHubProxy(hubName);
                    await hubConnection1.Start();

                    var hubConnection2 = new HubConnection(serverUrl2);
                    IHubProxy chatHubProxy2 = hubConnection2.CreateHubProxy(hubName);
                    chatHubProxy2.On<string>("broadcastMessage", tcs.SetResult);
                    await hubConnection2.Start();

                    const string expectedMessage = "Hello";
                    await chatHubProxy1.Invoke<string>("Send", expectedMessage);

                    if (await Task.WhenAny(tcs.Task, Task.Delay(5000)) != tcs.Task)
                    {
                        throw new TimeoutException("Timed out waiting for message");
                    }

                    Assert.Equal(expectedMessage, (await tcs.Task));
                }
            }
        }
    }

    [UsedImplicitly]
    public class ChatHub : Hub
    {
        public void Send(string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(message);
        }
    }
}
