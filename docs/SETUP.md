# Development setup and screen test

If both IDEs share one clone, pull main once. In Visual Studio open windows/Kitescreen.sln; in Android Studio open the same clone's android subfolder. Stop running apps before updating, then rebuild and run both. Protocol v3 requires both apps to be updated.

## Android
Gradle Wrapper 8.9 is included. Use JDK 17 and Android SDK 35. Sync, select Samsung SM-X200 and run. Keep Kitescreen visible and the tablet unlocked. Enable USB debugging and authorize your computer. Android should show "Waiting for Windows over USB".

## Windows
Use Visual Studio with .NET desktop development and the .NET 8 SDK. Build and run Kitescreen.Host. It finds adb.exe in standard SDK locations; use Browse if necessary.

1. Click Refresh devices.
2. Select the Windows screen to share.
3. Select the authorized Samsung tablet.
4. Click Start screen.
5. Move a window on the selected monitor and verify it appears on the tablet. The tablet preserves aspect ratio with black borders when needed.
6. Click Stop to end streaming.

An updated Android APK is also available in the Kitescreen-Android-debug artifact of a successful GitHub Actions build.

## What this version does
Mirrors an existing monitor using JPEG frames over USB, the tablet's reported pixel size and up to 20 fps. Windows shows measured fps and Mbps. It does not yet create a second Windows display, forward touch or reconnect automatically. Rotate the tablet and restart streaming if the activity restarts. A later H.264 transport and IddCx driver will build on this prototype.

## Checks
GitHub Actions builds Windows, checks its binary frame format, builds Android, runs protocol/lifecycle unit tests and Android lint. Physical image quality, capture behavior and USB performance still need testing on the user's PC and Samsung A8.
