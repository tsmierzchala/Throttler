using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

if (args.Length < 4)
{
    Console.WriteLine("Usage: <listenPort> <destinationHost> <destinationPort> <kbps> <packetLossProbability>");
    return;
}

int listenPort = int.Parse(args[0]);
string destinationHost = args[1];
int destinationPort = int.Parse(args[2]);
int kbps = int.Parse(args[3]);
double packetLossProbability = double.Parse(args[4]); // Parse packetLossProbability

SetupLogging();

TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
listener.Start();
Log($"Listening on port {listenPort}");

while (true)
{
    try
    {
        TcpClient client = await listener.AcceptTcpClientAsync();
        Log("Client connected");
        _ = Task.Run(() => HandleClient(client, destinationHost, destinationPort, kbps, packetLossProbability));
    }
    catch (Exception ex)
    {
        LogError($"Error: {ex.Message}");
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