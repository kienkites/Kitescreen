# One-install Windows package (release preparation)

The Windows installer source combines the WPF host, the root-enumerated IddCx display driver, and Android Platform-Tools (ADB). The elevated installer creates the virtual display and installs its driver automatically. Uninstall removes the Kitescreen virtual display. The Windows app uses the bundled ADB first; Android Studio and a separate Platform-Tools installation are not required on the customer's PC. A tablet still needs the Android Kitescreen app and authorization for USB debugging in this prototype.

**No customer-ready installer exists yet.** The `Kitescreen-Driver-VM-Test-Only` artifact is self-signed for VM tests and is rejected by `Build-Release.ps1`. The separate `Prepare driver signing submission` workflow validates the unsigned driver and, when manually dispatched, produces `Kitescreen-Driver-Submission-Unsigned`: a CAB intended for EV signing and submission to Microsoft Partner Center. The CAB itself is not an installer. After Microsoft returns the signed driver package, extract its INF, CAT and DLL into one folder and pass that folder to `Build-Release.ps1`. The script verifies that Microsoft signed the catalog **and that it covers both the INF and DLL**. Test signing, disabling Windows security settings or manually running devgen are not part of the customer install path.

Microsoft's attestation signature permits Windows 10/11 Desktop to trust a driver but does not certify its operation or publish it to Windows Update for retail audiences. Hardware Lab Kit testing and dashboard signing are the recommended route for general release. The owner of the EV code signing certificate must sign the submission CAB and submit it through their Hardware Developer Program account; this step cannot be completed by public CI. Keep the EV private key outside GitHub Actions.

On a Windows build machine with .NET 8 SDK, Inno Setup 6, a release-signed driver directory, and a Platform-Tools directory that you are authorized to redistribute:

```powershell
./installer/Build-Release.ps1 -DriverPackage C:\path\to\signed-driver -PlatformTools C:\path\to\platform-tools -Iscc 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' -SignTool 'C:\path\to\signtool.exe'
```

This creates `installer/out/Kitescreen-Windows-Setup.exe`. The build validates the catalog signer before compiling, and Windows validates the driver during installation. The installer requires Windows administrator approval once. Production signing of the installer executable and Windows 11 / real-tablet validation are still release gates. Driver registration, upgrading, rollback on failure and uninstall must be exercised on a spare Windows 11 PC before distribution. The current video transport is JPEG and video smoothness is not yet resolved.
