# Experimental Kitescreen Indirect Display Driver

Based on Microsoft's Windows Driver Samples, video/IndirectDisplay, under MS-PL. This source is **not installed, signed or used by the current app**. It is an isolated starting point for development with Visual Studio C++ desktop tools and the matching WDK. The Windows host build does not include it.

Changes from the sample: a single EDID-less display with target modes 1920 × 1200, 1200 × 1920 and 1280 × 800; a distinct prototype hardware ID and driver group; Kitescreen diagnostic name. The driver still discards each swapchain frame as the original sample does. Installing it will not stream any image to the Android app.

Next: read frames from the assigned swapchain, deliver them to a low-latency local encoder and add monitor arrival/departure tied to the authorized USB session. Then test a signed development package on Windows 11, including crash/disconnect recovery. Only after this can Kitescreen offer true Extend mode. Do not install this prototype on the user's main PC.
