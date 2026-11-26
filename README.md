# Kitescreen

Turn your Android tablet into a second display for Windows 11 via USB.

## Current preview: USB screen mirroring
Select an existing Windows monitor and view it on an Android tablet over a USB data cable.

- .NET 8 WPF host: monitor and USB device selection, JPEG capture on a background thread, fps/Mbps status.
- Kotlin Android receiver: bounded image decoding, full-screen aspect-preserving display.
- ADB transport: versioned handshake, frame acknowledgments, bounded memory and disconnect cleanup.
- Included Gradle Wrapper, platform builds, protocol tests and Android lint.

The stable preview mirrors an existing monitor at the tablet's reported pixel dimensions and up to 20 fps. An experimental IddCx driver and Extend frame path are in development, but Extend has not been validated on a real Windows 11 PC and tablet. H.264, audio, touch forwarding and automatic reconnect remain future work.

The [single Windows installer source](installer/README.md) bundles the host, Platform-Tools and a virtual display driver. A customer-ready installer depends on release signing and hardware validation; the CI test driver is not distributed in it.

Open `android/` in Android Studio and `windows/Kitescreen.sln` in Visual Studio.
See [setup and test instructions](docs/SETUP.md) and [wire protocol and limits](docs/USB-PROTOCOL.md).
