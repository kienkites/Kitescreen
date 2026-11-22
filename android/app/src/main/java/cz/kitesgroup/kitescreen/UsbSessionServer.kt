package cz.kitesgroup.kitescreen

import java.io.Closeable
import java.io.InputStream
import java.net.InetAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.Executors

/** Development control channel, reachable through adb forwarding only. */
class UsbSessionServer(private val status: (String) -> Unit) : Closeable {
    private val executor = Executors.newSingleThreadExecutor()
    @Volatile private var stopped = false
    private var listener: ServerSocket? = null
    private var client: Socket? = null

    fun start() {
        executor.execute {
            try {
                val bound = ServerSocket(27183, 1, InetAddress.getByName("127.0.0.1"))
                synchronized(this) {
                    if (stopped) { bound.close(); return@execute }
                    listener = bound
                }
                status("Waiting for Windows over USB…")
                while (!stopped) {
                    val socket = bound.accept()
                    synchronized(this) {
                        if (stopped) { socket.close(); return@execute }
                        client = socket
                    }
                    socket.use {
                        it.soTimeout = 10_000
                        it.tcpNoDelay = true
                        try {
                            val input = it.getInputStream()
                            val output = it.getOutputStream()
                            require(readLine(input) == "KITESCREEN/1 HELLO") { "Unsupported handshake" }
                            output.write("KITESCREEN/1 READY\n".toByteArray(Charsets.US_ASCII))
                            output.flush()
                            status("Connected to Windows via USB\nControl channel ready — video coming next.")
                            while (!stopped) {
                                require(readLine(input) == "PING") { "Invalid heartbeat" }
                                output.write("PONG\n".toByteArray(Charsets.US_ASCII))
                                output.flush()
                            }
                        } catch (e: Exception) {
                            if (!stopped) status("Disconnected. Waiting for Windows…")
                        } finally {
                            synchronized(this) { if (client === it) client = null }
                        }
                    }
                }
            } catch (e: Exception) {
                if (!stopped) status("Connection error: ${e.message}")
            }
        }
    }

    private fun readLine(input: InputStream): String {
        val text = StringBuilder()
        repeat(128) {
            val value = input.read()
            check(value >= 0) { "Connection closed" }
            if (value == 10) return text.toString()
            require(value in 32..126) { "Invalid control message" }
            text.append(value.toChar())
        }
        error("Control message too long")
    }

    override fun close() {
        synchronized(this) {
            stopped = true
            client?.close()
            listener?.close()
        }
        executor.shutdownNow()
    }
}
