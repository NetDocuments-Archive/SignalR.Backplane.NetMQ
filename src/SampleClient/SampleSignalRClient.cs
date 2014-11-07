namespace SampleClient
{
    using System;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Client;

    public class SampleSignalRClient : IDisposable
    {
        private readonly int _httpPort;
        private readonly Subject<string> _messages = new Subject<string>();
        private readonly IDisposable _subscription;
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _chatHubProxy;

        public SampleSignalRClient(int httpPort)
        {
            _httpPort = httpPort;
            string serverUrl = string.Format("http://localhost:{0}", httpPort);
            _hubConnection = new HubConnection(serverUrl);
            _chatHubProxy = _hubConnection.CreateHubProxy("ChatHub");
            _subscription = _chatHubProxy.On<string>("broadcastMessage", _messages.OnNext);
            _hubConnection.Start().Wait();
        }

        public IObservable<string> Messages
        {
            get { return _messages; }
        }

        public Task<string> Send(string message)
        {
            return _chatHubProxy.Invoke<string>("Send", string.Format("Via {0}: {1}", _httpPort, message));
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _hubConnection.Dispose();
        }
    }
}