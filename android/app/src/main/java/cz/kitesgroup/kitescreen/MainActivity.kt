package cz.kitesgroup.kitescreen

import android.app.Activity
import android.graphics.BitmapFactory
import android.graphics.Color
import android.os.Bundle
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.FrameLayout
import android.widget.ImageView
import android.widget.TextView
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicBoolean

class MainActivity : Activity() {
    private lateinit var message: TextView
    private lateinit var picture: ImageView
    @Volatile private var server: UsbSessionServer? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        @Suppress("DEPRECATION")
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_FULLSCREEN or
            View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
        picture = ImageView(this).apply {
            setBackgroundColor(Color.BLACK)
            scaleType = ImageView.ScaleType.FIT_CENTER
            contentDescription = "Windows screen preview"
        }
        message = TextView(this).apply {
            textSize = 15f
            setTextColor(Color.WHITE)
            setBackgroundColor(0xB0101827.toInt())
            gravity = Gravity.CENTER
            val padding = (12 * resources.displayMetrics.density).toInt()
            setPadding(padding, padding, padding, padding)
        }
        setContentView(FrameLayout(this).apply {
            addView(picture, FrameLayout.LayoutParams(-1, -1))
            addView(message, FrameLayout.LayoutParams(-1, -2, Gravity.TOP))
        })
    }

    override fun onStart() {
        super.onStart()
        lateinit var active: UsbSessionServer
        active = UsbSessionServer(
            dimensions = {
                val metrics = android.util.DisplayMetrics()
                @Suppress("DEPRECATION")
                windowManager.defaultDisplay.getRealMetrics(metrics)
                metrics.widthPixels to metrics.heightPixels
            },
            onFrame = { jpeg ->
                // Validate dimensions before allocating decoded pixels.
                val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
                BitmapFactory.decodeByteArray(jpeg, 0, jpeg.size, bounds)
                require(bounds.outWidth in 1..4096 && bounds.outHeight in 1..4096 &&
                    bounds.outWidth.toLong() * bounds.outHeight <= 4_194_304 &&
                    bounds.outMimeType == "image/jpeg") { "Unsupported frame dimensions or format" }
                val bitmap = requireNotNull(BitmapFactory.decodeByteArray(jpeg, 0, jpeg.size)) { "Cannot decode frame" }
                val accepted = AtomicBoolean(false)
                val applied = CountDownLatch(1)
                runOnUiThread {
                    try {
                        if (server === active) {
                            picture.setImageBitmap(bitmap)
                            message.visibility = View.GONE
                            accepted.set(true)
                        }
                    } finally { applied.countDown() }
                }
                check(applied.await(3, TimeUnit.SECONDS) && accepted.get()) { "Display is no longer active" }
            },
            status = { text ->
                runOnUiThread {
                    if (server === active) {
                        message.text = "Kitescreen · $text"
                        message.visibility = View.VISIBLE
                    }
                }
            }
        )
        server = active
        active.start()
    }

    override fun onStop() {
        val active = server
        server = null
        active?.close()
        picture.setImageDrawable(null)
        super.onStop()
    }
}
