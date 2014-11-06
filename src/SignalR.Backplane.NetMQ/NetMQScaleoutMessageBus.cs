namespace Signalr.Backplane.NetMQ
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using global::NetMQ;
    using global::NetMQ.Sockets;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Messaging;
    using Microsoft.AspNet.SignalR.Tracing;

    /// <summary>
    ///     the NetMQScaleoutMessageBus uses NetMQ (0MQ implementation) to distribute messages.
    ///     This class uses a publish subscribe pattern. Each node will listen to published messages on all other nodes.
    /// </summary>
    public class NetMQScaleoutMessageBus : ScaleoutMessageBus
    {
        private static long _latestMessageId;
        private readonly NetMQScaleoutConfiguration _configuration;
        private readonly List<NetMQSocket> _subscriberSockets = new List<NetMQSocket>();
        private readonly TraceSource _trace;
        private NetMQContext _context;
        private NetMQSocket _publisherSocket;
        private bool _running;

        public NetMQScaleoutMessageBus(IDependencyResolver resolver, NetMQScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            _configuration = configuration;
            _context = NetMQContext.Create();

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(NetMQScaleoutMessageBus).Name];

            SetupPublisher();

            ThreadPool.QueueUserWorkItem(Subscribe);
        }


        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            TraceMessages(messages, "Sending from " + _configuration.PublisherAddress);

            return Task.Factory.StartNew(() =>
            {
                long id = GetMessageId();
                var message = new NetMQMessage(id, messages);
                _publisherSocket.Send(message.GetBytes());

                SendMessageToSelf(streamIndex, id, message);
            });
        }

        private void SendMessageToSelf(int streamIndex, long id, NetMQMessage message)
        {
            Task.Factory.StartNew(() => OnReceived(streamIndex, (ulong) id, message.ScaleoutMessage));
        }

        private static long GetMessageId()
        {
            long id = Interlocked.Increment(ref _latestMessageId);
            return id;
        }

        private void SetupPublisher()
        {
            Open(0);

            _publisherSocket = _context.CreatePublisherSocket();
            _publisherSocket.Bind(_configuration.PublisherAddress);
        }

        private void Subscribe(object state)
        {
            _running = true;
            foreach(string subscriberAddress in _configuration.SubscriberAddresses)
            {
                SubscriberSocket subscriberSocket = _context.CreateSubscriberSocket();
                subscriberSocket.Connect(subscriberAddress);
                subscriberSocket.Subscribe("");

                _subscriberSockets.Add(subscriberSocket);
                Task.Factory.StartNew(() => WaitForMessages(subscriberSocket));
            }
        }

        private void WaitForMessages(NetMQSocket subscriberSocket)
        {
            while(_running)
            {
                byte[] bytes = subscriberSocket.Receive();

                NetMQMessage message = NetMQMessage.FromBytes(bytes);

                TraceMessages(message.ScaleoutMessage.Messages, "Receiving at " + _configuration.PublisherAddress);
                OnReceived(0, (ulong) message.MessageId, message.ScaleoutMessage);
            }
        }

        private void TraceMessages(IEnumerable<Message> messages, string messageType)
        {
            if(!_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                return;
            }

            foreach(Message message in messages)
            {
                _trace.TraceVerbose("{0} {1} bytes over Service Bus: {2}", messageType, message.Value.Array.Length,
                    message.GetString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(_publisherSocket != null)
                {
                    _publisherSocket.Dispose();
                    _publisherSocket = null;
                }

                foreach(NetMQSocket subscriber in _subscriberSockets.ToArray())
                {
                    subscriber.Dispose();
                    _subscriberSockets.Remove(subscriber);
                }

                // Setting running to false will stop the subscriber tasks
                _running = false;

                if(_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}