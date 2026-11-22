# Development setup

Pull main in both clones if Visual Studio and Android Studio use different folders.

## Android
Open the android subfolder (not the repository root) in Android Studio. Gradle Wrapper 8.9 is included, including its official JAR verified against the published SHA-256. No separate Gradle installation is needed. Use JDK 17 and install SDK 35 when prompted. Sync, select the tablet and run the app.

On Samsung A8: Settings > About tablet > Software information > tap Build number seven times. Enable USB debugging in Developer options. Connect a USB data cable, unlock the tablet and authorize your computer. Keep Kitescreen visible.

## Windows
Open windows/Kitescreen.sln in Visual Studio with the .NET desktop development workload and .NET 8 SDK. Build and run Kitescreen.Host.

The app searches standard Android SDK locations for platform-tools/adb.exe. If missing, use Browse and select the adb.exe from the SDK configured in Android Studio.

Click Refresh devices, select your authorized Samsung tablet, then Connect. Both apps should report connected. Disconnect ends the session. If unauthorized, allow debugging on the tablet and refresh. If the handshake times out, make sure the Android app is running in the foreground.

This milestone only tests the USB control channel. No screen image, extended desktop, audio or touch forwarding is implemented yet.

## Build checks
GitHub Actions builds the Windows host and Android debug APK and runs Android lint. Hardware tests are separate: follow docs/USB-PROTOCOL.md. An APK can be downloaded from a successful build's Kitescreen-Android-debug artifact.
