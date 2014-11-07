namespace SampleServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Client;
    using Microsoft.AspNet.SignalR.Hubs;
    using Microsoft.Owin.Hosting;
    using Owin;
    using Signalr.Backplane.NetMQ;

    public class SignalRSampleServer : IDisposable
    {
        private readonly IDisposable _httpServer;
        private readonly IHubProxy _hubProxy;
        private const string HubName = "ChatHub";
        private readonly Subject<string> _messages = new Subject<string>();
        private readonly IDisposable _subscription;

        public SignalRSampleServer(int httpPort, int netMQPort, IEnumerable<int> subscriberPorts)
        {
            string serverUrl = string.Format("http://localhost:{0}", httpPort);
            string netMQAddress = string.Format("tcp://127.0.0.1:{0}", netMQPort);
            var subscriberAddresses = subscriberPorts
                .Select(p => string.Format("tcp://127.0.0.1:{0}", p))
                .ToArray();
            var config = new NetMQScaleoutConfiguration(netMQAddress, subscriberAddresses);

            _httpServer = WebApp.Start(serverUrl, app =>
            {
                var resolver = new DefaultDependencyResolver();
                resolver.UseNetMQServiceBus(config);
                var assemblyLocator = new AssemblyLocator();
                resolver.Register(typeof(IAssemblyLocator), () => assemblyLocator);
                var hubConfiguration = new HubConfiguration
                {
                    Resolver = resolver,
                };
                app.MapSignalR(hubConfiguration);
            });
            var hubConnection = new HubConnection(serverUrl);
            _hubProxy = hubConnection.CreateHubProxy(HubName);
            _subscription = _hubProxy.On<string>("broadcastMessage", _messages.OnNext);
            hubConnection.Start().Wait();
        }

        public IHubProxy HubProxy
        {
            get { return _hubProxy; }
        }

        public IObservable<string> Messages
        {
            get { return _messages; }
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _httpServer.Dispose();
        }

        private class AssemblyLocator : IAssemblyLocator
        {
            public IList<Assembly> GetAssemblies()
            {
                return new[] { typeof(ChatHub).Assembly };
            }
        }
    }
}