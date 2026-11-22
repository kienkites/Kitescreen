# Kitescreen

Turn your Android tablet into a second display for Windows 11 via USB.

## Current milestone: USB control connection
- Kotlin Android receiver with a foreground loopback control server.
- .NET 8 WPF Windows host with USB device selection, ADB forwarding, handshake and heartbeat.
- Gradle Wrapper and GitHub Actions platform build checks.

Video streaming, virtual display driver, touch forwarding and automatic reconnect are not implemented yet. This is a development preview, not a working second display.

Open `android/` in Android Studio and `windows/Kitescreen.sln` in Visual Studio.
See [setup](docs/SETUP.md), [USB protocol and test checklist](docs/USB-PROTOCOL.md), and [architecture](docs/ARCHITECTURE.md).
