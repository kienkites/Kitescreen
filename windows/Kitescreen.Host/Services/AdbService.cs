using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Kitescreen.Host.Services;

public sealed record AndroidDevice(string Serial, string State, string Model)
{
    public string Display => $"{Model} — {Serial} ({State})";
    public bool Authorized => State == "device";
}

public sealed class AdbService(string executable)
{
    public static string DefaultPath
    {
        get
        {
            foreach (var root in new[] {
                Environment.GetEnvironmentVariable("ANDROID_HOME"),
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk") })
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                var candidate = Path.Combine(root, "platform-tools", "adb.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return "adb";
        }
    }

    public async Task<IReadOnlyList<AndroidDevice>> DevicesAsync(CancellationToken token)
    {
        var output = await RunAsync(token, "devices", "-l");
        var devices = new List<AndroidDevice>();
        foreach (var line in output.Split('\n'))
        {
            var parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 2 || parts[0] == "List" || line.StartsWith('*')) continue;
            // Exclude emulators and network transports in this USB-only milestone.
            if (parts[0].StartsWith("emulator-") || parts[0].Contains(':') || parts[0].Contains("._tcp")) continue;
            if (parts[1] is not ("device" or "unauthorized" or "offline")) continue;
            var model = parts.FirstOrDefault(p => p.StartsWith("model:"))?[6..].Replace('_', ' ') ?? "Android";
            devices.Add(new(parts[0], parts[1], model));
        }
        return devices;
    }

    public async Task<int> ForwardAsync(string serial, CancellationToken token)
    {
        var result = await RunAsync(token, "-s", serial, "forward", "tcp:0", "tcp:27183");
        if (!int.TryParse(result.Trim(), out var port) || port is < 1 or > 65535)
            throw new IOException("ADB did not return a valid forwarding port.");
        return port;
    }

    public Task RemoveForwardAsync(string serial, int port) =>
        RunAsync(CancellationToken.None, "-s", serial, "forward", "--remove", $"tcp:{port}");

    private async Task<string> RunAsync(CancellationToken token, params string[] arguments)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var start = new ProcessStartInfo(executable) {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new IOException("USB connection tool not found. Reinstall Kitescreen, or check Android SDK Platform-Tools on this development PC.", e);
        }
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            await process.WaitForExitAsync();
            await Task.WhenAll(output, error);
            token.ThrowIfCancellationRequested();
            throw new TimeoutException("ADB timed out. Check the USB cable and tablet authorization.");
        }
        var stdout = await output;
        var stderr = await error;
        if (process.ExitCode != 0) throw new IOException($"ADB: {stderr.Trim()}");
        return stdout;
    }
}
