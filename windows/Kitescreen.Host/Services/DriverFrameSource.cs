using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Kitescreen.Host.Services;

/// <summary>Receives the newest IddCx swapchain pixels from the local UMDF driver.</summary>
public sealed class DriverFrameSource : IDisposable
{
    private const string PipeName = "KitescreenFrames";
    private readonly NamedPipeServerStream pipe;
    private readonly ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
    private readonly EncoderParameters parameters = new(1);
    private byte[] pixels = Array.Empty<byte>();
    private readonly byte[] header = new byte[16];

    private DriverFrameSource(NamedPipeServerStream pipe)
    {
        this.pipe = pipe;
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);
    }

    public static async Task<DriverFrameSource> ConnectAsync(Action<string> status, CancellationToken token)
    {
        var security = new PipeSecurity();
        var user = WindowsIdentity.GetCurrent().User ?? throw new IOException("Cannot determine Windows user.");
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
        // UMDF normally runs as LocalService; System is included for other supported host configurations.
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        var pipe = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous, 65536, 0, security);
        try
        {
            status("Waiting for Kitescreen virtual monitor frames. Install the signed driver, then choose Extend in Windows Display settings.");
            await pipe.WaitForConnectionAsync(token);
            return new DriverFrameSource(pipe);
        }
        catch { pipe.Dispose(); throw; }
    }

    public async Task<byte[]> CaptureAsync(int targetWidth, int targetHeight, CancellationToken token)
    {
        await pipe.ReadExactlyAsync(header, token);
        if (header[0] != 'K' || header[1] != 'F' || header[2] != 'R' || header[3] != '1')
            throw new IOException("Invalid virtual display frame.");
        var width = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));
        var size = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12, 4));
        if (width is < 640 or > 4096 || height is < 480 or > 4096 ||
            (long)width * height > 4_194_304 || size != (long)width * height * 4)
            throw new IOException("Invalid virtual display dimensions.");
        if (pixels.Length != size) pixels = new byte[size];
        await pipe.ReadExactlyAsync(pixels, token);

        using var source = new Bitmap(width, height, PixelFormat.Format32bppRgb);
        var locked = source.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
        try
        {
            if (locked.Stride == width * 4) Marshal.Copy(pixels, 0, locked.Scan0, size);
            else for (var y = 0; y < height; y++)
                Marshal.Copy(pixels, y * width * 4, IntPtr.Add(locked.Scan0, y * locked.Stride), width * 4);
        }
        finally { source.UnlockBits(locked); }

        using var output = new MemoryStream();
        if (width == targetWidth && height == targetHeight)
            source.Save(output, jpeg, parameters);
        else
        {
            using var scaled = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
            using var graphics = Graphics.FromImage(scaled);
            graphics.Clear(Color.Black);
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            var ratio = Math.Min((double)targetWidth / width, (double)targetHeight / height);
            var w = Math.Max(1, (int)Math.Round(width * ratio));
            var h = Math.Max(1, (int)Math.Round(height * ratio));
            graphics.DrawImage(source, new Rectangle((targetWidth - w) / 2, (targetHeight - h) / 2, w, h));
            scaled.Save(output, jpeg, parameters);
        }
        if (output.Length > FrameProtocol.MaxFrameBytes)
            throw new IOException("Virtual display frame exceeds the USB protocol limit.");
        return output.ToArray();
    }

    public void Dispose()
    {
        parameters.Dispose();
        pipe.Dispose();
    }
}
