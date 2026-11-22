# Architecture and implementation order

1. Completed: validate the two application builds on Windows and Android.
2. Completed: discover authorized USB devices through adb and establish a versioned session using port forwarding.
3. Completed preview: bounded JPEG mirroring at tablet-reported pixel dimensions; latency remains visible on video.
4. Current experimental Extend path: IddCx virtual monitor copies frames through a local pipe to the Windows host, then uses the existing JPEG/USB receiver. Driver load, frame correctness and tablet output require physical testing before claiming Extend works.
5. Add touch forwarding, orientation changes and reconnect handling.

Windows will encode video; Android will decode and render it. Touch events will travel back on a control channel. Keep encoding, transport and UI separate. Bind development TCP listeners to loopback only. Do not expose adb over a network.

Initial target: one Windows 11 PC and one Android tablet over USB. SM-X200 reports 1920 × 1200 in landscape. 30 fps and low latency remain unverified goals. Driver installation, signing and hardware validation are separate from the app build. Audio and Wi-Fi are outside the initial scope.
