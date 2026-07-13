package com.enterprise.billing_system.mobile_app

import android.os.Bundle
import android.content.Context
import io.flutter.embedding.android.FlutterActivity

class MainActivity : FlutterActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        
        // Clear bloated image cache keys from SharedPreferences to prevent OutOfMemoryError
        try {
            val sharedPref = getSharedPreferences("FlutterSharedPreferences", Context.MODE_PRIVATE)
            val editor = sharedPref.edit()
            val keysToRemove = mutableListOf<String>()
            
            for (entry in sharedPref.all.entries) {
                val key = entry.key
                if (key.startsWith("flutter.cached_img_")) {
                    keysToRemove.add(key)
                }
            }
            
            if (keysToRemove.isNotEmpty()) {
                for (key in keysToRemove) {
                    editor.remove(key)
                }
                editor.apply()
            }
        } catch (e: Exception) {
            // Ignore errors
        }
    }
}
