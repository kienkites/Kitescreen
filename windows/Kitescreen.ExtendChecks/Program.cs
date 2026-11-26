using System.Buffers.Binary;
using System.Drawing;
using System.IO.Pipes;
using Kitescreen.Host.Services;

using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var connection = DriverFrameSource.ConnectAsync(_ => { }, deadline.Token);
using var sender = new NamedPipeClientStream(".", "KitescreenFrames", PipeDirection.Out, PipeOptions.Asynchronous);
await sender.ConnectAsync(deadline.Token);
using var receiver = await connection;

const int width = 640, height = 480;
var frame = new byte[16 + width * height * 4];
"KFR1"u8.CopyTo(frame);
BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4, 4), width);
BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(8, 4), height);
BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(12, 4), width * height * 4);
for (var i = 16; i < frame.Length; i += 4)
{
    frame[i] = 0x20; // B
    frame[i + 1] = 0x80; // G
    frame[i + 2] = 0xE0; // R
    frame[i + 3] = 0xFF;
}
var read = receiver.CaptureAsync(width, height, deadline.Token);
await sender.WriteAsync(frame, deadline.Token);
var jpeg = await read;
using var image = new Bitmap(new MemoryStream(jpeg));
if (image.Width != width || image.Height != height)
    throw new Exception("Virtual frame size was not preserved.");
var pixel = image.GetPixel(100, 100);
if (pixel.R < 200 || pixel.G < 90 || pixel.G > 170 || pixel.B > 90)
    throw new Exception($"Virtual frame color mismatch: {pixel}.");
Console.WriteLine($"Virtual frame pipe and JPEG verified: {image.Width} × {image.Height}, {jpeg.Length} bytes.");

// An absent driver must produce an actionable error rather than leaving the UI waiting forever.
sender.Dispose();
receiver.Dispose();
try
{
    await DriverFrameSource.ConnectAsync(_ => { }, CancellationToken.None, TimeSpan.FromMilliseconds(200));
    throw new Exception("Extend unexpectedly connected without a virtual display driver.");
}
catch (IOException e) when (e.Message.Contains("Extend is not ready"))
{
    Console.WriteLine("Missing virtual display driver times out with a clear error.");
}
