# USB control channel v1

This milestone establishes communication, not a virtual display or video stream.

Windows invokes SDK adb with argument lists (no shell). It lists physical device candidates, asks the user to select an authorized one, then runs `adb -s SERIAL forward tcp:0 tcp:27183`. ADB assigns an available host port. Android listens on IPv4 loopback port 27183 only while the app is visible.

ASCII messages end in LF. Receivers reject non-printable bytes and lines without LF within 128 bytes.

1. Windows sends `KITESCREEN/1 HELLO\n`.
2. Android replies `KITESCREEN/1 READY\n`.
3. Windows sends `PING\n`; Android replies `PONG\n` every two seconds.
4. EOF, invalid messages or read timeouts close the session. Windows has a five-second message timeout; Android has a ten-second idle timeout.
5. Windows removes only the forward port it created on disconnect/error. Android closes sockets when the activity stops.

The protocol contains no screen data, remote commands, or touch injection. Do not use TCP/IP adb for this USB-only preview. USB candidate filtering excludes emulator and typical network serial formats; validate detection on actual Samsung hardware.

## Manual checks

- Authorized A8 + Android app visible: Windows reports connected and Android reports connected.
- Authorization denied: device shown unauthorized; connection cannot start.
- App closed: connection fails with a useful message.
- Unplug USB during heartbeat: session exits within timeout; controls become usable again.
- Disconnect button: forwarding removed, Android returns to waiting.
- Background Android: sockets close; foreground returns to waiting.
- Two tablets: explicitly select one; forwarding uses that serial.
- Launch another Android instance on same port: error is shown, never silently report connected.

Platform builds and these hardware checks must pass before release.
