using Kitescreen.Host.Services;

using var output = new MemoryStream();
// Fixed wire fixture consumed identically by Android DataInputStream.
await FrameProtocol.WriteAsync(output, [0xFF, 0xD8, 0xFF, 0xD9], CancellationToken.None);
if (!output.ToArray().SequenceEqual(new byte[] { 0, 0, 0, 4, 0xFF, 0xD8, 0xFF, 0xD9 }))
    throw new Exception("Wire format must be big-endian length followed by unchanged payload.");
foreach (var payload in new[] { Array.Empty<byte>(), new byte[FrameProtocol.MaxFrameBytes + 1] })
{
    using var invalid = new MemoryStream();
    try
    {
        await FrameProtocol.WriteAsync(invalid, payload, CancellationToken.None);
        throw new Exception("Invalid frame accepted.");
    }
    catch (IOException)
    {
        if (invalid.Length != 0) throw new Exception("Invalid frame partially written.");
    }
}
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try
{
    await FrameProtocol.WriteAsync(output, [1], canceled.Token);
    throw new Exception("Cancellation was ignored.");
}
catch (OperationCanceledException) { }
Console.WriteLine("PASS: Android-compatible wire fixture, frame limits, cancellation.");
