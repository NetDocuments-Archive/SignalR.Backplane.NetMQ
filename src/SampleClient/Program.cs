namespace SampleClient
{
    using System;
    using PowerArgs;

    public class Program
    {
        private static void Main(string[] args)
        {
            var settings = Args.Parse<Settings>(args);
            using(var client = new SampleSignalRClient(settings.HttpPort))
            {
                using(client.Messages.Subscribe(Console.WriteLine))
                {
                    Console.WriteLine("Client connected to HTTP {0}.", settings.HttpPort);

                    while(true)
                    {
                        Console.Write("> ");
                        var message = Console.ReadLine();
                        if(message == "exit")
                        {
                            break;
                        }
                        client.Send(message).Wait();
                    }
                }
            }
        }
    }
}