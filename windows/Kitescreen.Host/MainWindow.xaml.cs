using System.ComponentModel;
using System.Windows;
using Kitescreen.Host.Services;
using Microsoft.Win32;

namespace Kitescreen.Host;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? session;
    private bool closed;
    private Task? sessionTask;

    public MainWindow()
    {
        InitializeComponent();
        AdbPath.Text = AdbService.DefaultPath;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "ADB executable|adb.exe", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) AdbPath.Text = dialog.FileName;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, false);
        try
        {
            var devices = await new AdbService(AdbPath.Text.Trim()).DevicesAsync(lifetime.Token);
            if (closed) return;
            Devices.ItemsSource = devices;
            Devices.SelectedItem = devices.FirstOrDefault(d => d.Authorized) ?? devices.FirstOrDefault();
            Status.Text = devices.Count == 0 ? "No USB tablets found. Check the data cable and USB debugging."
                : devices.Any(d => d.Authorized) ? "Select your tablet and click Connect."
                : "Unlock the tablet and allow USB debugging, then refresh.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed) Status.Text = ex.Message; }
        finally { if (!closed) SetBusy(false, false); }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (Devices.SelectedItem is not AndroidDevice device || !device.Authorized)
        {
            Status.Text = "Refresh devices and select an authorized USB tablet.";
            return;
        }
        using var active = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        session = active;
        SetBusy(true, true);
        Status.Text = "Connecting… Keep Kitescreen open on the tablet.";
        try
        {
            sessionTask = new UsbSession().RunAsync(new AdbService(AdbPath.Text.Trim()), device.Serial,
                text => { if (!closed) Status.Text = text; }, active.Token);
            await sessionTask;
        }
        catch (OperationCanceledException)
        {
            if (!closed) Status.Text = active.IsCancellationRequested ? "Disconnected." : "Connection timed out. Open Kitescreen on the tablet and retry.";
        }
        catch (Exception ex) { if (!closed) Status.Text = $"Connection failed: {ex.Message}"; }
        finally
        {
            session = null;
            sessionTask = null;
            if (!closed) SetBusy(false, false);
        }
    }

    private void Disconnect_Click(object sender, RoutedEventArgs e) => session?.Cancel();

    private void SetBusy(bool busy, bool connected)
    {
        RefreshButton.IsEnabled = ConnectButton.IsEnabled = BrowseButton.IsEnabled = !busy;
        AdbPath.IsEnabled = Devices.IsEnabled = !busy;
        DisconnectButton.IsEnabled = connected;
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (closed) return;
        closed = true;
        var pending = sessionTask;
        // Keep the dispatcher alive until the owned adb forwarding port is removed.
        e.Cancel = pending != null;
        lifetime.Cancel();
        if (pending != null)
        {
            try { await pending; } catch (Exception) { /* Connect handler reports errors. */ }
            Close();
        }
    }
}
