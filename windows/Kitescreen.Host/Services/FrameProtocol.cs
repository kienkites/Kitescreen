using System.Buffers.Binary;
using System.IO;

namespace Kitescreen.Host.Services;

public static class FrameProtocol
{
    public const int MaxFrameBytes = 4 * 1024 * 1024;

    public static async Task WriteAsync(Stream stream, byte[] jpeg, CancellationToken token)
    {
        if (jpeg.Length is < 1 or > MaxFrameBytes) throw new IOException("Invalid frame size.");
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, jpeg.Length);
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(jpeg, token);
    }
}
