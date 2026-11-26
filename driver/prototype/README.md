# Experimental Kitescreen indirect display driver

Based on Microsoft's Windows Driver Samples, video/IndirectDisplay (MS-PL). This is a user-mode IddCx driver for one virtual display. It advertises 1920 × 1200, 1200 × 1920 and 1280 × 800 at 60 Hz. The driver copies swapchain pixels to a local named pipe (`KitescreenFrames`); the Windows host reads them, compresses JPEG and sends them to Android using the existing USB session.

## State and testing

The driver build is checked separately in GitHub Actions, but compilation does not establish that it loads, that the virtual monitor activates, or that video arrives on a real tablet. CI produces a **test-signed VM-only package** with INF, DLL, CAT and a public test certificate. It is not a production-signed installer. Do not install this experimental display driver on the primary PC. Use a Windows 11 x64 test VM with graphics support or a spare machine, and take a snapshot before installing. Windows must trust the test certificate (LocalMachine Root and TrustedPublisher), and the test machine must allow test-signed drivers. If Secure Boot or local policy blocks test signing, stop and use another test setup; do not change the main PC's boot security settings. The root device ID is `Root\KitescreenPrototypeDisplay`. Staging the INF with PnPUtil alone does not create a root-enumerated device; a development device must be created using the WDK `devgen` tool or a suitable device installer.

The [submission workflow](https://github.com/kienkites/Kitescreen/actions/workflows/driver-submission.yml) builds a separate unsigned CAB with INF, DLL, PDB and catalog. Pull requests validate the CAB; only a manual dispatch uploads it. The CAB needs EV signing and Microsoft dashboard processing before it can be included in the [single Windows installer](../../installer/README.md). Never substitute the test-signed VM package for the Microsoft-signed package.

On the test machine, download the `Kitescreen-Driver-VM-Test-Only` artifact from the successful [driver CI workflow](https://github.com/kienkites/Kitescreen/actions/workflows/driver.yml) and extract it. After preparing test certificate trust and test-signing according to Microsoft guidance, run an elevated developer prompt with the WDK `devgen` tool and stage the signed package:

```powershell
devgen /add /bus ROOT /hardwareid "Root\KitescreenPrototypeDisplay"
pnputil /add-driver ".\IddSampleDriver.inf" /install
```

Confirm Device Manager shows the Kitescreen prototype display without an error and that Windows Display settings offer its 1920 × 1200 mode. Select **Extend**, start the Android receiver, then choose **Extend (driver)** in the Windows host. Move a window onto the new desktop and check that the tablet displays it. The host waits for the driver's frames; clicking Stop cancels the wait. Driver installation and Windows display arrangement remain manual in this prototype. Keep the instance ID printed by `devgen` so the test device can be removed with `pnputil /remove-device <instance-id>` and the test driver removed from the driver store after testing.

## Remaining work

The local pipe transfers raw BGRA, then the host performs JPEG compression. This prioritizes proving that the tablet displays the *extended* desktop. Video smoothness is not solved yet. Add hardware H.264 encoding/decoding and cursor metadata next. Then tie virtual monitor arrival/departure to the authorized USB session, handle orientation dynamically and package a properly signed installer before general use.
