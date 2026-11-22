# Development setup

## Get the source

Pull `main` in your existing clone. If Android Studio and Visual Studio use separate clones, pull in both folders.

## Windows

Install Visual Studio with the .NET desktop development workload and .NET 8 SDK. Open `windows/Kitescreen.sln`, select Kitescreen.Host as the startup project and run. The expected result is a status screen only.

## Android

Baseline toolchain: JDK 17, Gradle 8.9, Android Gradle Plugin 8.7.3, Kotlin 2.0.21 and Android SDK 35. These are pinned baseline versions, not a claim to use the latest releases. Minimum device version: Android 8.0 (API 26).

This initial skeleton does not include the binary Gradle wrapper. Before syncing, install Gradle 8.9 and run the following inside `android/`:

```powershell
gradle wrapper --gradle-version 8.9 --distribution-type bin
```

Commit the generated `gradlew`, `gradlew.bat` and `gradle/wrapper/` files so later clones do not need a local Gradle installation. Then open `android/` in Android Studio, select JDK 17 for Gradle, install SDK 35 if prompted, sync and run the app. The expected result is a welcome screen only.

USB debugging is needed when deploying the development app over USB. The USB streaming implementation will be added separately.

## Validation status

Source skeleton only. Neither platform build nor hardware connection has been verified in the assistant environment. Record actual build results before treating this as a tested release.
