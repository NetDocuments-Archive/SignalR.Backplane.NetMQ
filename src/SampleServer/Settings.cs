namespace SampleServer
{
    using PowerArgs;

    public class Settings
    {
        [ArgRequired]
        public int HttpPort { get; set; }


        [ArgRequired]
        public int NetMQPort { get; set; }

        [ArgRequired]
        public int[] SubscriberPorts { get; set; }
    }
}