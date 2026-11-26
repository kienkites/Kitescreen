using System.ComponentModel;
using System.Windows;
using Kitescreen.Host.Services;

namespace Kitescreen.Host;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource lifetime = new();
    private readonly AdbService adb = new(AdbService.DefaultPath);
    private CancellationTokenSource? session;
    private bool closed;
    private Task? sessionTask;

    public MainWindow()
    {
        InitializeComponent();
        Displays.ItemsSource = DisplaySource.List();
        Displays.SelectedIndex = 0;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        SetBusy(true, false);
        try
        {
            var selectedScreen = (Displays.SelectedItem as DisplaySource)?.Name;
            var screens = DisplaySource.List();
            Displays.ItemsSource = screens;
            Displays.SelectedItem = screens.FirstOrDefault(screen => screen.Name == selectedScreen) ?? screens.FirstOrDefault();
            var selectedTablet = (Devices.SelectedItem as AndroidDevice)?.Serial;
            var devices = await adb.DevicesAsync(lifetime.Token);
            if (closed) return;
            Devices.ItemsSource = devices;
            Devices.SelectedItem = devices.FirstOrDefault(d => d.Serial == selectedTablet && d.Authorized)
                ?? devices.FirstOrDefault(d => d.Authorized) ?? devices.FirstOrDefault();
            Status.Text = devices.Count == 0 ? "No USB tablets found. Check the data cable and USB debugging."
                : devices.Any(d => d.Authorized) ? "Select your tablet and click Connect."
                : "Unlock the tablet and allow USB debugging, then refresh.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed) Status.Text = ex.Message; }
        finally { if (!closed) SetBusy(false, false); }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e) => await ConnectAsync(ExtendMode.IsChecked == true);

    private async Task ConnectAsync(bool extend)
    {
        if (Devices.SelectedItem is not AndroidDevice device || !device.Authorized)
        {
            Status.Text = "Refresh devices and select an authorized USB tablet.";
            return;
        }
        if (!extend && Displays.SelectedItem is not DisplaySource)
        {
            Status.Text = "Select a Windows screen to share.";
            return;
        }
        using var active = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        session = active;
        SetBusy(true, true);
        Status.Text = "Connecting… Keep Kitescreen open on the tablet.";
        try
        {
            sessionTask = new UsbSession().RunAsync(adb, device.Serial,
                extend ? null : (DisplaySource)Displays.SelectedItem,
                text => Dispatcher.BeginInvoke(new Action(() => { if (!closed) Status.Text = text; })), active.Token);
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
        RefreshButton.IsEnabled = ConnectButton.IsEnabled = ConnectionOptions.IsEnabled = !busy;
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
