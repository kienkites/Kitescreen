using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kitescreen.Host.Services;

public sealed class UsbSession
{
    public Task RunAsync(AdbService adb, string serial, DisplaySource? display, Action<string> status, CancellationToken token) =>
        Task.Run(async () =>
        {
            int? port = null;
            try
            {
                // In Extend mode wait for IddCx frames before opening the tablet session.
                using var virtualSource = display is null ? await DriverFrameSource.ConnectAsync(status, token) : null;
                port = await adb.ForwardAsync(serial, token);
                using var client = new TcpClient { NoDelay = true };
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    deadline.CancelAfter(TimeSpan.FromSeconds(5));
                    await client.ConnectAsync(IPAddress.Loopback, port.Value, deadline.Token);
                }
                using var stream = client.GetStream();
                await WriteLineAsync(stream, "KITESCREEN/3 HELLO", token);
                if (await ReadLineAsync(stream, token) != "KITESCREEN/3 READY")
                    throw new IOException("Incompatible Android app. Update and run both apps.");
                var dimensions = (await ReadLineAsync(stream, token)).Split(' ');
                if (dimensions.Length != 3 || dimensions[0] != "DISPLAY" ||
                    !int.TryParse(dimensions[1], out var width) || !int.TryParse(dimensions[2], out var height) ||
                    width is < 640 or > 4096 || height is < 480 or > 4096 || (long)width * height > 4_194_304)
                    throw new IOException("Tablet reported an invalid display resolution.");
                await WriteLineAsync(stream, "STREAM JPEG", token);
                if (await ReadLineAsync(stream, token) != "STREAM READY")
                    throw new IOException("The tablet did not accept screen streaming.");

                using var capture = display is null ? null : new ScreenCapture(display, width, height);
                var statistics = Stopwatch.StartNew();
                int frames = 0;
                long bytes = 0;
                double captureMs = 0;
                double deliveryMs = 0;
                status(display is null ? "Tablet connected. Waiting for the first extended-display image…"
                    : $"Streaming mirrored screen at {width} × {height}…");
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var started = Stopwatch.GetTimestamp();
                    var jpeg = virtualSource is null ? capture!.Capture()
                        : await virtualSource.CaptureAsync(width, height, token);
                    captureMs += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    var delivering = Stopwatch.GetTimestamp();
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        deadline.CancelAfter(TimeSpan.FromSeconds(5));
                        await FrameProtocol.WriteAsync(stream, jpeg, deadline.Token);
                        if (await ReadLineAsync(stream, deadline.Token) != "FRAME")
                            throw new IOException("Tablet rejected the frame.");
                    }
                    deliveryMs += Stopwatch.GetElapsedTime(delivering).TotalMilliseconds;
                    frames++;
                    bytes += jpeg.Length;
                    if (statistics.Elapsed.TotalSeconds >= 1)
                    {
                        status($"Streaming: {frames / statistics.Elapsed.TotalSeconds:F1} fps · {bytes * 8 / statistics.Elapsed.TotalSeconds / 1_000_000:F1} Mbps · capture/JPEG {captureMs / frames:F0} ms · USB/decode {deliveryMs / frames:F0} ms");
                        frames = 0;
                        bytes = 0;
                        captureMs = 0;
                        deliveryMs = 0;
                        statistics.Restart();
                    }
                    var pause = TimeSpan.FromMilliseconds(1000d / 30) - Stopwatch.GetElapsedTime(started);
                    if (pause > TimeSpan.Zero) await Task.Delay(pause, token);
                }
            }
            finally
            {
                if (port.HasValue)
                {
                    try { await adb.RemoveForwardAsync(serial, port.Value); }
                    catch (Exception e) when (e is IOException or TimeoutException)
                    { status($"USB forwarding cleanup failed: {e.Message}"); }
                }
            }
        }, token);

    private static async Task WriteLineAsync(NetworkStream stream, string line, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        await stream.WriteAsync(Encoding.ASCII.GetBytes(line + "\n"), deadline.Token);
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var message = new StringBuilder();
        var buffer = new byte[1];
        for (var i = 0; i < 128; i++)
        {
            if (await stream.ReadAsync(buffer.AsMemory(), deadline.Token) == 0)
                throw new IOException("Tablet disconnected.");
            var value = buffer[0];
            if (value == 10) return message.ToString();
            if (value is < 32 or > 126) throw new IOException("Invalid control message.");
            message.Append((char)value);
        }
        throw new IOException("Control message too long.");
    }
}
