# Architecture and implementation order

1. Completed: validate the two application builds on Windows and Android.
2. Completed: discover authorized USB devices through adb and establish a versioned session using port forwarding.
3. Current: bounded JPEG frame preview and disconnect cleanup at tablet-reported pixel dimensions. Replace with H.264 hardware encode/decode after measuring USB latency.
4. Add the IddCx virtual monitor and integrate its frames with the encoder for extended desktop mode.
5. Add touch forwarding, orientation changes and reconnect handling.

Windows will encode video; Android will decode and render it. Touch events will travel back on a control channel. Keep encoding, transport and UI separate. Bind development TCP listeners to loopback only. Do not expose adb over a network.

Initial target: one Windows 11 PC and one Android tablet over USB. SM-X200 reports 1920 × 1200 in landscape. 30 fps and low latency remain unverified goals. Audio and Wi-Fi are outside the initial scope.
