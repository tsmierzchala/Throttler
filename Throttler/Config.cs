namespace Throttler
{
    public class Config
    {
        public int ListenPort { get; set; } = 5999;
        public string DestinationHost { get; set; } = "localhost";
        public int DestinationPort { get; set; } = 5999;
        public int Kbps { get; set; } = 100;
        public double PacketLossProbability { get; set; } = 0.1;
        public int BufferSize { get; set; } = 1024;
    }
}
