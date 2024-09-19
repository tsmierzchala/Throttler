using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using Throttler;

var config = LoadConfig();
if (config == null)
{
    Console.WriteLine("Failed to load the configuration file.");
    return;
}

SetupLogging();

TcpListener listener = new TcpListener(IPAddress.Any, config.ListenPort);
listener.Start();
Log($"Listening on port {config.ListenPort}");

while (true)
{
    try
    {
        TcpClient client = await listener.AcceptTcpClientAsync();
        Log("Client connected");
        _ = Task.Run(() => HandleClient(client, config.DestinationHost, config.DestinationPort, config.Kbps, config.PacketLossProbability));
    }
    catch (Exception ex)
    {
        LogError($"Error: {ex.Message}");
    }
}

static Config? LoadConfig()
{
    try
    {
        string json = File.ReadAllText("config.json");
        var config = JsonSerializer.Deserialize<Config>(json);

        if (config == null)
        {
            LogError("Deserialization resulted in null. Returning default config.");
            return new Config();  // Return a default config to prevent null references
        }

        return config;
    }
    catch (Exception ex)
    {
        LogError($"Error loading config: {ex.Message}");
        return new Config();
    }
}

static async Task HandleClient(TcpClient client, string destHost, int destPort, int kbps, double packetLossProbability)
{
    using (client)
    {
        try
        {
            using TcpClient destination = new TcpClient(destHost, destPort);
            Log($"Connected to {destHost}:{destPort}");

            using NetworkStream clientStream = client.GetStream();
            using NetworkStream destStream = destination.GetStream();

            Task clientToDest = TransferData(clientStream, destStream, kbps, packetLossProbability);
            Task destToClient = TransferData(destStream, clientStream, kbps, packetLossProbability);

            await Task.WhenAny(clientToDest, destToClient);
            Log("Data transfer completed");
        }
        catch (Exception ex)
        {
            LogError($"Connection error: {ex.Message}");
        }
    }
}
static async Task TransferData(Stream source, Stream destination, int kbps, double packetLossProbability)
{
    byte[] buffer = new byte[1024]; // Increase buffer size for efficiency
    int maxBytesPerInterval = (kbps * 1024) / 8; // Calculate max bytes per second
    int interval = 1000; // Check every second
    int bytesTransferred = 0;
    var stopwatch = new System.Diagnostics.Stopwatch();
    Random random = new Random();

    stopwatch.Start();
    while (true)
    {
        // Calculate bytes read in this iteration
        int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) break;

        // Simulate packet loss
        if (random.NextDouble() >= packetLossProbability)
        {
            await destination.WriteAsync(buffer, 0, bytesRead);
            await destination.FlushAsync();
        }
        else
        {
            Log($"Dropped {bytesRead} bytes");
        }

        // Throttle based on bytes transferred and time passed
        bytesTransferred += bytesRead;
        Log($"Transferred {bytesRead} bytes");

        if (bytesTransferred >= maxBytesPerInterval)
        {
            int elapsed = (int)stopwatch.ElapsedMilliseconds;
            if (elapsed < interval)
            {
                await Task.Delay(interval - elapsed);
            }
            stopwatch.Restart();
            bytesTransferred = 0;
        }
    }
}

static void SetupLogging()
{
    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
    Trace.AutoFlush = true;
}

static void Log(string message)
{
    Trace.WriteLine($"[INFO] {DateTime.Now}: {message}");
}

static void LogError(string message)
{
    Trace.WriteLine($"[ERROR] {DateTime.Now}: {message}");
}