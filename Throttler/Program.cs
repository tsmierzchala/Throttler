using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Text.Json;
using Throttler;
using System;

var config = LoadConfig();
if (config == null)
{
    Console.WriteLine("Failed to load the configuration file.");
    return;
}

SetupLogging();

TcpListener listener = new TcpListener(IPAddress.Any, config.ListenPort);
try
{
    listener.Start();
    Log($"Listening on port {config.ListenPort}");
    while (true)
    {
        try
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Log("Client connected");
            _ = Task.Run(() => HandleClient(client, config.DestinationHost, config.DestinationPort, config.Kbps, config.PacketLossProbability, config.BufferSize));
        }
        catch (SocketException socketEx)
        {
            LogError($"Socket error: {socketEx.Message}");
        }
        catch (IOException ioEx)
        {
            LogError($"IO error: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            LogError($"Unexpected error: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    LogError($"Could not start listener: {ex.Message}");
    listener.Stop();
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
    catch (FileNotFoundException)
    {
        LogError("Configuration file not found. Returning default config.");
    }
    catch (JsonException jsonEx)
    {
        LogError($"JSON format error: {jsonEx.Message}. Returning default config.");
    }
    catch (Exception ex)
    {
        LogError($"Error loading config: {ex.Message}");
    }
    return new Config();
}
static async Task HandleClient(TcpClient client, string destHost, int destPort, int kbps, double packetLossProbability, int bufferSize)
{
    using (client)
    {
        try
        {
            using TcpClient destination = new TcpClient(destHost, destPort);
            Log($"Connected to {destHost}:{destPort}");
            using NetworkStream clientStream = client.GetStream();
            using NetworkStream destStream = destination.GetStream();
            Task clientToDest = TransferData(clientStream, destStream, kbps, packetLossProbability, bufferSize);
            Task destToClient = TransferData(destStream, clientStream, kbps, packetLossProbability, bufferSize);
            await Task.WhenAny(clientToDest, destToClient);
            Log("Data transfer completed");
        }
        catch (SocketException socketEx)
        {
            LogError($"Socket error while handling client: {socketEx.Message}");
        }
        catch (IOException ioEx)
        {
            LogError($"IO error while handling client: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            LogError($"Connection error: {ex.Message}");
        }
    }
}
static async Task TransferData(Stream source, Stream destination, int kbps, double packetLossProbability, int bufferSize)
{
    byte[] buffer = new byte[bufferSize];
    int maxBytesPerInterval = (kbps * 1024) / 8;
    int interval = 1000; // ms
    int bytesTransferred = 0;
    var stopwatch = new Stopwatch();
    Random random = new Random();
    stopwatch.Start();
    while (true)
    {
        int bytesRead;
        try
        {
            bytesRead = await source.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;
        }
        catch (IOException ioEx)
        {
            LogError($"Read error: {ioEx.Message}");
            break;
        }
        if (random.NextDouble() >= packetLossProbability)
        {
            try
            {
                await destination.WriteAsync(buffer, 0, bytesRead);
                await destination.FlushAsync();
            }
            catch (IOException ioEx)
            {
                LogError($"Write error: {ioEx.Message}");
                break;
            }
        }
        else
        {
            Log($"Dropped {bytesRead} bytes");
        }
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