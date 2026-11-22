package cz.kitesgroup.kitescreen

import org.junit.Assert.*
import org.junit.Test
import java.io.ByteArrayInputStream
import java.io.DataInputStream
import java.io.EOFException

class FrameProtocolTest {
    @Test fun rejectsZeroNegativeAndOversizedFrames() {
        for (size in listOf(0, -1, FrameProtocol.MAX_FRAME_BYTES + 1)) {
            val header = byteArrayOf((size ushr 24).toByte(), (size ushr 16).toByte(),
                (size ushr 8).toByte(), size.toByte())
            try {
                FrameProtocol.readFrame(DataInputStream(ByteArrayInputStream(header)))
                fail("Invalid frame must be rejected before allocation")
            } catch (_: IllegalArgumentException) { }
        }
    }

    @Test(expected = EOFException::class)
    fun rejectsTruncatedPayload() {
        FrameProtocol.readFrame(DataInputStream(ByteArrayInputStream(byteArrayOf(0, 0, 0, 4, 1, 2))))
    }
}
