package cz.kitesgroup.kitescreen

import org.junit.Assert.*
import org.junit.Test
import java.io.DataOutputStream
import java.net.Socket
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

class UsbSessionServerTest {
    private fun withServer(onFrame: (ByteArray) -> Unit = {}, test: (UsbSessionServer) -> Unit) {
        val ready = CountDownLatch(1)
        val server = UsbSessionServer(onFrame, dimensions = { 1920 to 1200 }) { if (it.startsWith("Waiting")) ready.countDown() }
        try {
            server.start()
            assertTrue("Server must bind loopback", ready.await(3, TimeUnit.SECONDS))
            test(server)
        } finally { server.close() }
    }

    @Test fun handshakeAndHeartbeat() = withServer {
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            val reader = socket.getInputStream().bufferedReader(Charsets.US_ASCII)
            socket.getOutputStream().write("KITESCREEN/3 HELLO\nPING\nPING\n".toByteArray())
            assertEquals("KITESCREEN/3 READY", reader.readLine())
            assertEquals("DISPLAY 1920 1200", reader.readLine())
            assertEquals("PONG", reader.readLine())
            assertEquals("PONG", reader.readLine())
        }
    }

    @Test fun rejectsOversizedHandshakeAndAcceptsNextClient() = withServer {
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            socket.getOutputStream().write("A".repeat(128).toByteArray())
            assertEquals(-1, socket.getInputStream().read())
        }
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            socket.getOutputStream().write("KITESCREEN/3 HELLO\n".toByteArray())
            assertEquals("KITESCREEN/3 READY", socket.getInputStream().bufferedReader().readLine())
        }
    }

    @Test fun closingServerClosesActiveSession() = withServer { server ->
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            socket.getOutputStream().write("KITESCREEN/3 HELLO\n".toByteArray())
            val reader = socket.getInputStream().bufferedReader()
            assertEquals("KITESCREEN/3 READY", reader.readLine())
            assertEquals("DISPLAY 1920 1200", reader.readLine())
            server.close()
            assertNull(reader.readLine())
        }
    }

    @Test fun acknowledgesFramesAfterDeliveryAndRejectsInvalidLength() {
        var delivered: ByteArray? = null
        withServer(onFrame = { delivered = it }) {
            Socket("127.0.0.1", 27183).use { socket ->
                socket.soTimeout = 3000
                val output = DataOutputStream(socket.getOutputStream())
                val reader = socket.getInputStream().bufferedReader()
                output.writeBytes("KITESCREEN/3 HELLO\nSTREAM JPEG\n")
                assertEquals("KITESCREEN/3 READY", reader.readLine())
            assertEquals("DISPLAY 1920 1200", reader.readLine())
                assertEquals("STREAM READY", reader.readLine())
                val payload = byteArrayOf(1, 2, 3, 4)
                output.writeInt(payload.size)
                // Fragmented delivery must not be mistaken for a complete frame.
                output.write(payload, 0, 2)
                output.write(payload, 2, 2)
                assertEquals("FRAME", reader.readLine())
                assertArrayEquals(payload, delivered)
                output.writeInt(FrameProtocol.MAX_FRAME_BYTES + 1)
                assertNull(reader.readLine())
            }
        }
    }
}
