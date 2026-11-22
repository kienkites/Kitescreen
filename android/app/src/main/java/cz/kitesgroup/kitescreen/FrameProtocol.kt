package cz.kitesgroup.kitescreen

import java.io.DataInputStream

/** Each JPEG is prefixed by a signed, big-endian 32-bit byte count. */
object FrameProtocol {
    const val MAX_FRAME_BYTES = 4 * 1024 * 1024

    fun readFrame(input: DataInputStream): ByteArray {
        val length = input.readInt()
        require(length in 1..MAX_FRAME_BYTES) { "Invalid frame size" }
        return ByteArray(length).also { input.readFully(it) }
    }
}
