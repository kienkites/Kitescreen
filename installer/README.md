# One-install Windows package (release preparation)

The Windows installer source combines the WPF host, the root-enumerated IddCx display driver, and Android Platform-Tools (ADB). The elevated installer creates the virtual display and installs its driver automatically. Uninstall removes the Kitescreen virtual display. The Windows app uses the bundled ADB first; Android Studio and a separate Platform-Tools installation are not required on the customer's PC. A tablet still needs the Android Kitescreen app and authorization for USB debugging in this prototype.

**No customer-ready installer exists yet.** The driver CI artifact is self-signed for VM tests and is rejected by `Build-Release.ps1`. The build script requires a complete production driver package with a Microsoft release-signed catalog. Test signing, disabling Windows security settings or manually running devgen are not part of the customer install path.

On a Windows build machine with .NET 8 SDK, Inno Setup 6, a release-signed driver directory, and a Platform-Tools directory that you are authorized to redistribute:

```powershell
./installer/Build-Release.ps1 -DriverPackage C:\path\to\signed-driver -PlatformTools C:\path\to\platform-tools -Iscc 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

This creates `installer/out/Kitescreen-Windows-Setup.exe`. The build validates the catalog signer before compiling, and Windows validates the driver during installation. The installer requires Windows administrator approval once. Production signing of the installer executable and Windows 11 / real-tablet validation are still release gates. Driver registration, upgrading, rollback on failure and uninstall must be exercised on a spare Windows 11 PC before distribution. The current video transport is JPEG and video smoothness is not yet resolved.
