// ReSharper disable once CheckNamespace
namespace Microsoft.AspNet.SignalR
{
    using Microsoft.AspNet.SignalR.Messaging;
    using Signalr.Backplane.NetMQ;

    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver UseNetMQServiceBus(this IDependencyResolver resolver, NetMQScaleoutConfiguration configuration)
        {
            var bus = new NetMQScaleoutMessageBus(resolver, configuration);
            resolver.Register(typeof(IMessageBus), () => bus);
            return resolver;
        }
    }
}