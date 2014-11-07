namespace SampleClient
{
    using PowerArgs;

    public class Settings
    {
        [ArgRequired]
        public int HttpPort { get; set; }

    }
}