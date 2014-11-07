namespace SampleServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Hubs;
    using Microsoft.Owin.Hosting;
    using Owin;
    using Signalr.Backplane.NetMQ;

    public class SampleSignalRServer : IDisposable
    {
        private readonly IDisposable _httpServer;

        public SampleSignalRServer(int httpPort, int netMQPort, IEnumerable<int> subscriberPorts)
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
        }

        public void Dispose()
        {
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