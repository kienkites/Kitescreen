# Kitescreen

Turn your Android tablet into a second display for Windows 11 via USB.

## Development status

Initial project skeleton. USB video streaming, virtual display driver and touch forwarding are not implemented yet.

## Projects

- `android/`: Kotlin Android receiver. Open this folder in Android Studio.
- `windows/`: .NET 8 WPF host. Open `windows/Kitescreen.sln` in Visual Studio.
- `driver/`: planned Windows IddCx virtual display driver.
- `docs/`: setup and architecture notes.

See [development setup](docs/SETUP.md).
