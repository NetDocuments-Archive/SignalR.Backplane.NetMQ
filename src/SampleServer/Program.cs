namespace SampleServer
{
    using System;
    using PowerArgs;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            var settings = Args.Parse<Settings>(args);
            using(new SampleSignalRServer(settings.HttpPort, settings.NetMQPort, settings.SubscriberPorts))
            {
                Console.WriteLine("Server running on Http {0}, NetMQ {1}. Subscribing to {2}." ,
                    settings.HttpPort, settings.NetMQPort, string.Join(",", settings.SubscriberPorts));
                Console.ReadLine();
            }
        }
    }
}