# Virtual display driver (planned)

No driver implementation or installer is included yet.

The intended implementation uses C++ / UMDF / IddCx to expose a virtual monitor to Windows. This is required for a real extended desktop. Streaming an existing monitor alone only provides mirroring.

Development requires compatible Visual Studio C++ tools, Windows SDK and WDK. Driver signing and installation will be addressed with the driver implementation; do not change Windows signing settings for this skeleton.
