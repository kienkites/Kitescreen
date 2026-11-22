package cz.kitesgroup.kitescreen

import org.junit.Assert.*
import org.junit.Test
import java.net.Socket
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

class UsbSessionServerTest {
    private fun withServer(test: (UsbSessionServer) -> Unit) {
        val ready = CountDownLatch(1)
        val server = UsbSessionServer { if (it.startsWith("Waiting")) ready.countDown() }
        try {
            server.start()
            assertTrue("Server must bind loopback", ready.await(3, TimeUnit.SECONDS))
            test(server)
        } finally { server.close() }
    }

    @Test fun handshakeAndHeartbeat() = withServer {
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            val writer = socket.getOutputStream()
            val reader = socket.getInputStream().bufferedReader(Charsets.US_ASCII)
            writer.write("KITESCREEN/1 HELLO\nPING\nPING\n".toByteArray(Charsets.US_ASCII))
            writer.flush()
            assertEquals("KITESCREEN/1 READY", reader.readLine())
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
            socket.getOutputStream().write("KITESCREEN/1 HELLO\n".toByteArray())
            assertEquals("KITESCREEN/1 READY", socket.getInputStream().bufferedReader().readLine())
        }
    }

    @Test fun closingServerClosesActiveSession() = withServer { server ->
        Socket("127.0.0.1", 27183).use { socket ->
            socket.soTimeout = 3000
            socket.getOutputStream().write("KITESCREEN/1 HELLO\n".toByteArray())
            val reader = socket.getInputStream().bufferedReader()
            assertEquals("KITESCREEN/1 READY", reader.readLine())
            server.close()
            assertNull(reader.readLine())
        }
    }
}
