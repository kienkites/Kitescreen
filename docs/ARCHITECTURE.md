# Architecture and implementation order

1. Validate the two application builds on Windows and Android.
2. Discover authorized USB devices through adb and establish a versioned session using port forwarding.
3. Implement bounded video packets, H.264 decoding and disconnect cleanup; validate with test frames and then capture an existing Windows display.
4. Add the IddCx virtual monitor and integrate its frames with the encoder for extended desktop mode.
5. Add touch forwarding, orientation changes and reconnect handling.

Windows will encode video; Android will decode and render it. Touch events will travel back on a control channel. Keep encoding, transport and UI separate. Bind development TCP listeners to loopback only. Do not expose adb over a network.

Initial target: one Windows 11 PC and one Android tablet over USB. 1080p at 30 fps is a performance goal, not a verified capability. Audio and Wi-Fi are outside the initial scope.
