# Windows extended-display driver

A real extended desktop requires an Indirect Display Driver (IDD) exposing a monitor to Windows. The current app captures an existing monitor through GDI and cannot turn the tablet into an independent workspace.

Microsoft's [IddSample](https://github.com/microsoft/Windows-driver-samples/tree/main/video/IndirectDisplay) is the starting reference. Its swapchain processor discards frames; installing it unchanged would show a monitor in Windows but would not deliver its image to Kitescreen. A working Kitescreen driver must publish the assigned swapchain frames to the Windows host encoder through a versioned, bounded local channel and report the monitor as absent when the tablet disconnects. It should negotiate a 1920 x 1200 mode for SM-X200 and device-specific modes for other tablets.

Installation, driver signing and user-mode capture of the new display need end-to-end Windows 11 testing. Do not install the unmodified sample or disable driver signature enforcement to test the current mirroring build.
