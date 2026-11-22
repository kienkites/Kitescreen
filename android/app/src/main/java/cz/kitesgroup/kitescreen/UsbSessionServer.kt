package cz.kitesgroup.kitescreen

import java.io.Closeable
import java.io.DataInputStream
import java.io.InputStream
import java.net.InetAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.Executors

/** USB development transport. Never binds to a network-facing address. */
class UsbSessionServer(
    private val onFrame: (ByteArray) -> Unit = {},
    private val dimensions: () -> Pair<Int, Int> = { 1920 to 1200 },
    private val status: (String) -> Unit
) : Closeable {
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
                            val input = DataInputStream(it.getInputStream())
                            val output = it.getOutputStream()
                            fun reply(line: String) {
                                output.write((line + "\n").toByteArray(Charsets.US_ASCII))
                                output.flush()
                            }
                            require(readLine(input) == "KITESCREEN/3 HELLO") { "Update both Kitescreen apps" }
                            reply("KITESCREEN/3 READY")
                            val (width, height) = dimensions()
                            require(width in 640..4096 && height in 480..4096 && width.toLong() * height <= 4_194_304)
                            reply("DISPLAY $width $height")
                            while (!stopped) {
                                when (readLine(input)) {
                                    "PING" -> reply("PONG")
                                    "STREAM JPEG" -> {
                                        reply("STREAM READY")
                                        status("Receiving Windows screen via USB")
                                        while (!stopped) {
                                            val jpeg = FrameProtocol.readFrame(input)
                                            onFrame(jpeg)
                                            // No next frame until the UI has accepted this one.
                                            reply("FRAME")
                                        }
                                    }
                                    else -> error("Invalid command")
                                }
                            }
                        } catch (e: Exception) {
                            if (!stopped) status("Disconnected: ${e.message}\nWaiting for Windows…")
                        } finally {
                            synchronized(this) { if (client === it) client = null }
                        }
                    }
                }
            } catch (e: Exception) {
                if (!stopped) status("Connection error: ${e.message}")
            } finally {
                synchronized(this) {
                    client?.close()
                    listener?.close()
                }
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
