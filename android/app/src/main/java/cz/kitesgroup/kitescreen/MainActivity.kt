package cz.kitesgroup.kitescreen

import android.app.Activity
import android.os.Bundle
import android.view.Gravity
import android.view.WindowManager
import android.widget.TextView

class MainActivity : Activity() {
    private lateinit var message: TextView
    private var server: UsbSessionServer? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        message = TextView(this).apply {
            textSize = 22f
            gravity = Gravity.CENTER
            val padding = (24 * resources.displayMetrics.density).toInt()
            setPadding(padding, padding, padding, padding)
        }
        setContentView(message)
    }

    override fun onStart() {
        super.onStart()
        val active = UsbSessionServer { text ->
            runOnUiThread { if (server != null) message.text = "Kitescreen\n\n$text" }
        }
        server = active
        active.start()
    }

    override fun onStop() {
        val active = server
        server = null
        active?.close()
        super.onStop()
    }
}
