using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kitescreen.Host.Services;

public sealed class UsbSession
{
    public async Task RunAsync(AdbService adb, string serial, Action<string> status, CancellationToken token)
    {
        int? port = null;
        try
        {
            port = await adb.ForwardAsync(serial, token);
            using var client = new TcpClient { NoDelay = true };
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(IPAddress.Loopback, port.Value, deadline.Token);
            }
            using var stream = client.GetStream();
            await WriteAsync(stream, "KITESCREEN/1 HELLO", token);
            if (await ReadAsync(stream, token) != "KITESCREEN/1 READY")
                throw new IOException("Incompatible Android app. Update both apps.");
            status("Connected via USB. Control channel ready; video is not implemented yet.");
            while (true)
            {
                await WriteAsync(stream, "PING", token);
                if (await ReadAsync(stream, token) != "PONG") throw new IOException("Invalid heartbeat.");
                await Task.Delay(TimeSpan.FromSeconds(2), token);
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
    }

    private static async Task WriteAsync(NetworkStream stream, string line, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        await stream.WriteAsync(Encoding.ASCII.GetBytes(line + "\n"), deadline.Token);
    }

    private static async Task<string> ReadAsync(NetworkStream stream, CancellationToken token)
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
