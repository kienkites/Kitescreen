# USB screen protocol v3

This preview mirrors one selected existing Windows monitor. It does not create an extended display.

## Transport
ADB forwards an automatically allocated host TCP port to Android port 27183. Android listens on IPv4 loopback only while visible. Windows removes its own forwarding port on stop, error and application exit. No Wi-Fi or network-facing listener is enabled.

## Handshake
Printable ASCII messages end with LF. Lines without LF within 128 bytes are rejected.

1. Windows: `KITESCREEN/3 HELLO\n`.
2. Android: `KITESCREEN/3 READY\n`.
3. Android: `DISPLAY W H\n` with actual display pixels from its current orientation; Windows validates limits.
4. Windows: `STREAM JPEG\n`.
5. Android: `STREAM READY\n`.

Before streaming, Android also accepts `PING\n` and returns `PONG\n`. Earlier apps are incompatible; update both platforms.

## Frames
Each frame is a four-byte big-endian signed length followed by exactly that many JPEG bytes. Valid lengths: 1 through 4,194,304. Android validates JPEG dimensions (both at most 4096 pixels and at most 4,194,304 pixels in total) and MIME type before pixel allocation.

Android replies `FRAME\n` after decoding and queueing the bitmap for display. The UI keeps only the newest pending bitmap, so a busy UI drops stale frames. This acknowledgment does not confirm physical display scan-out. Windows waits for it before capturing the next frame, so the transport cannot accumulate an unbounded frame queue. Capture runs off the Windows UI thread.

Windows limits capture to a tablet's reported pixel size, JPEG quality 88 and at most 30 fps. The frame deadline is five seconds; Android has a ten-second socket idle timeout and no UI submission wait. EOF, invalid sizes, decode errors or missing acknowledgments terminate the session. There is no automatic reconnect yet.

## Manual checks
- Select each Windows monitor, including monitors to the left of the primary and mixed DPI screens.
- Move a window and pointer; verify the tablet shows the selected monitor with correct aspect ratio.
- Rotate the tablet, then click Start screen again after its activity restarts.
- Stop, unplug USB, background the Android app and close the Windows app: verify resources are released and manual reconnect works.
- Verify status fps responds to a slow receiver without growing delay or freezing Windows controls. Note Windows status readings for average capture/JPEG and USB/decode milliseconds; compare them while moving a window.
- Deny USB authorization: Start screen must refuse the unauthorized tablet.
- An old Android version must fail to connect with a clear instruction to update.

## Limits
Native pixel dimensions do not make mirroring an extended monitor. The image is centered without enlargement if the Windows source is smaller than the tablet; larger sources are scaled down. This JPEG prototype prioritizes a verifiable USB image path over production performance. Text is lossy, frame rate is limited, and a simple circular pointer marker is shown. Audio, touch, remote input, automatic reconnect, H.264, virtual monitor creation, HDR and protected content capture are not supported. Secure desktop and protected video may appear black or fail capture.
