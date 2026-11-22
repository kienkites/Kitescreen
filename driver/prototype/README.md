# Experimental Kitescreen indirect display driver

Based on Microsoft's Windows Driver Samples, video/IndirectDisplay (MS-PL). This is a user-mode IddCx driver for one virtual display. It advertises 1920 × 1200, 1200 × 1920 and 1280 × 800 at 60 Hz. The driver copies swapchain pixels to a local named pipe (`KitescreenFrames`); the Windows host reads them, compresses JPEG and sends them to Android using the existing USB session.

## State and testing

The driver build is checked separately in GitHub Actions, but compilation does not establish that it loads, that the virtual monitor activates, or that video arrives on a real tablet. The repository does **not** distribute a signed installer. Do not install an unsigned or untested driver on the primary PC. Test the development package on a Windows 11 VM or spare machine first, with a trusted test certificate and Windows test-signing requirements handled according to Microsoft driver guidance. The root device ID is `Root\KitescreenPrototypeDisplay`. Staging the INF with PnPUtil alone does not create a root-enumerated device; a development device must be created using the WDK `devgen` tool or a suitable device installer.

Once Windows reports a working Kitescreen virtual display, select **Extend** in Display settings, start the Android receiver, then choose **Extend (driver)** in the Windows host. The host waits for the driver's frames; clicking Stop cancels the wait. Driver installation and Windows display arrangement remain manual in this prototype.

## Remaining work

The local pipe transfers raw BGRA, then the host performs JPEG compression. This prioritizes proving that the tablet displays the *extended* desktop. Video smoothness is not solved yet. Add hardware H.264 encoding/decoding and cursor metadata next. Then tie virtual monitor arrival/departure to the authorized USB session, handle orientation dynamically and package a properly signed installer before general use.
