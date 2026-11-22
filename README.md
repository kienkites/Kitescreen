# Kitescreen

Turn your Android tablet into a second display for Windows 11 via USB.

## Current preview: USB screen mirroring
Select an existing Windows monitor and view it on an Android tablet over a USB data cable.

- .NET 8 WPF host: monitor and USB device selection, JPEG capture on a background thread, fps/Mbps status.
- Kotlin Android receiver: bounded image decoding, full-screen aspect-preserving display.
- ADB transport: versioned handshake, frame acknowledgments, bounded memory and disconnect cleanup.
- Included Gradle Wrapper, platform builds, protocol tests and Android lint.

The preview mirrors an existing monitor at at the tablet's reported pixel dimensions and up to 20 fps. It does not yet create an extended desktop. H.264, the virtual display driver, audio, touch forwarding and automatic reconnect remain future work.

Open `android/` in Android Studio and `windows/Kitescreen.sln` in Visual Studio.
See [setup and test instructions](docs/SETUP.md) and [wire protocol and limits](docs/USB-PROTOCOL.md).
