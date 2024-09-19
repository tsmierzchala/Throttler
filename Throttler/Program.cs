using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

if (args.Length < 4)
{
    Console.WriteLine("Usage: <listenPort> <destinationHost> <destinationPort> <kbps>");
    return;
}

int listenPort = int.Parse(args[0]);
string destinationHost = args[1];
int destinationPort = int.Parse(args[2]);
int kbps = int.Parse(args[3]);

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
        _ = Task.Run(() => HandleClient(client, destinationHost, destinationPort, kbps));
    }
    catch (Exception ex)
    {
        LogError($"Error: {ex.Message}");
    }
}

static async Task HandleClient(TcpClient client, string destHost, int destPort, int kbps)
{
    using (client)
    {
        try
        {
            using TcpClient destination = new TcpClient(destHost, destPort);
            Log($"Connected to {destHost}:{destPort}");

            using NetworkStream clientStream = client.GetStream();
            using NetworkStream destStream = destination.GetStream();

            Task clientToDest = TransferData(clientStream, destStream, kbps);
            Task destToClient = TransferData(destStream, clientStream, kbps);

            await Task.WhenAny(clientToDest, destToClient);
            Log("Data transfer completed");
        }
        catch (Exception ex)
        {
            LogError($"Connection error: {ex.Message}");
        }
    }
}
static async Task TransferData(Stream source, Stream destination, int kbps)
{
    byte[] buffer = new byte[1024]; // Increase buffer size for efficiency
    int maxBytesPerInterval = (kbps * 1024) / 8; // Calculate max bytes per second
    int interval = 1000; // Check every second
    int bytesTransferred = 0;
    var stopwatch = new System.Diagnostics.Stopwatch();

    stopwatch.Start();
    while (true)
    {
        // Calculate bytes read in this iteration
        int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) break;

        bytesTransferred += bytesRead;
        await destination.WriteAsync(buffer, 0, bytesRead);
        await destination.FlushAsync();

        Log($"Transferred {bytesRead} bytes");

        // Throttle based on bytes transferred and time passed
        if (bytesTransferred >= maxBytesPerInterval)
        {
            int elapsed = (int)stopwatch.ElapsedMilliseconds;

            // If elapsed time is less than interval, delay the remaining time
            if (elapsed < interval)
            {
                await Task.Delay(interval - elapsed);
            }

            // Reset measurements for next interval
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