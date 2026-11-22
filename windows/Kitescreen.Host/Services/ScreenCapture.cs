using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Forms = System.Windows.Forms;

namespace Kitescreen.Host.Services;

public sealed record DisplaySource(string Name, Rectangle Bounds, bool Primary)
{
    public string Display => $"{Name} — {Bounds.Width} × {Bounds.Height}{(Primary ? " (primary)" : "")}";
    public static DisplaySource[] List() => Forms.Screen.AllScreens
        .Select(screen => new DisplaySource(screen.DeviceName, screen.Bounds, screen.Primary))
        .OrderByDescending(screen => screen.Primary).ToArray();
}

/// <summary>Bounded JPEG capture prototype. Created and used on a background thread.</summary>
public sealed class ScreenCapture : IDisposable
{
    private readonly Rectangle bounds;
    private readonly Bitmap desktop;
    private readonly Bitmap scaled;
    private readonly Graphics desktopGraphics;
    private readonly Graphics scaledGraphics;
    private readonly ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
    private readonly EncoderParameters parameters = new(1);

    public ScreenCapture(DisplaySource source)
    {
        bounds = source.Bounds;
        if (bounds.Width < 1 || bounds.Height < 1 || (long)bounds.Width * bounds.Height > 33_554_432)
            throw new IOException("Unsupported monitor size. Select a monitor up to 32 megapixels.");
        var ratio = Math.Min(1d, Math.Min(1280d / bounds.Width, 1280d / bounds.Height));
        desktop = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        scaled = new Bitmap(Math.Max(1, (int)(bounds.Width * ratio)), Math.Max(1, (int)(bounds.Height * ratio)), PixelFormat.Format24bppRgb);
        desktopGraphics = Graphics.FromImage(desktop);
        scaledGraphics = Graphics.FromImage(scaled);
        scaledGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
    }

    public byte[] Capture()
    {
        desktopGraphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        // Include a simple pointer marker without taking ownership of the Windows cursor handle.
        var pointer = Forms.Cursor.Position;
        if (bounds.Contains(pointer))
        {
            var x = pointer.X - bounds.X;
            var y = pointer.Y - bounds.Y;
            desktopGraphics.FillEllipse(Brushes.Black, x - 6, y - 6, 12, 12);
            desktopGraphics.DrawEllipse(Pens.White, x - 6, y - 6, 12, 12);
        }
        scaledGraphics.DrawImage(desktop, new Rectangle(0, 0, scaled.Width, scaled.Height));
        using var output = new MemoryStream();
        scaled.Save(output, jpeg, parameters);
        if (output.Length > FrameProtocol.MaxFrameBytes) throw new IOException("Captured frame exceeds the protocol limit.");
        return output.ToArray();
    }

    public void Dispose()
    {
        parameters.Dispose();
        scaledGraphics.Dispose();
        desktopGraphics.Dispose();
        scaled.Dispose();
        desktop.Dispose();
    }
}
