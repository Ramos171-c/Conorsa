import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ImageCacheService {
  static const String _keyPrefix = 'cached_img_';

  /// Pre-cache a list of image URLs in the background
  static Future<void> cacheImages(List<String> imageUrls) async {
    if (kIsWeb) return; // Web browsers handle HTTP image caching natively
    for (final url in imageUrls) {
      if (url.isEmpty || url.contains('default-product.png')) continue;
      // Skip if already cached
      if (await isCached(url)) continue;
      
      // Cache in background
      _downloadAndCache(url);
    }
  }

  /// Check if image is cached locally
  static Future<bool> isCached(String imageUrl) async {
    if (kIsWeb) return false;
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.containsKey('$_keyPrefix$imageUrl');
    } catch (_) {
      return false;
    }
  }

  /// Get cached image bytes
  static Future<String?> getCachedImageBase64(String imageUrl) async {
    if (imageUrl.isEmpty || kIsWeb) return null;
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getString('$_keyPrefix$imageUrl');
    } catch (_) {
      return null;
    }
  }

  /// Download and cache single image
  static Future<void> _downloadAndCache(String imageUrl) async {
    if (kIsWeb) return;
    try {
      final response = await http.get(Uri.parse(imageUrl)).timeout(const Duration(seconds: 15));
      if (response.statusCode == 200) {
        final base64Image = base64Encode(response.bodyBytes);
        final prefs = await SharedPreferences.getInstance();

        // Evict any stale cached image for the same product (same UUID, different ticks)
        _evictStaleProductImage(prefs, imageUrl);

        await prefs.setString('$_keyPrefix$imageUrl', base64Image);
      }
    } catch (_) {
      // Ignore background download errors or quota limits safely
    }
  }

  /// Remove any previously cached image that belongs to the same product UUID
  static void _evictStaleProductImage(SharedPreferences prefs, String newUrl) {
    try {
      // Extract the product UUID from a path like /uploads/products/<UUID>_<ticks>.jpg
      final uri = Uri.parse(newUrl);
      final segments = uri.pathSegments;
      if (segments.length < 2) return;
      final filename = segments.last; // e.g. "25d0b2c8-b2d5-4cda-9322-3642a6dc46a7_639226638255167943.jpg"
      final underscoreIdx = filename.indexOf('_');
      if (underscoreIdx <= 0) return;
      final productId = filename.substring(0, underscoreIdx); // the UUID part

      // Find all cached keys that reference the same product but with a different filename
      final keysToDelete = prefs
          .getKeys()
          .where((k) => k.startsWith(_keyPrefix) && k.contains(productId) && !k.endsWith(filename))
          .toList();

      for (final k in keysToDelete) {
        prefs.remove(k);
      }
    } catch (_) {}
  }

  /// Force cache single image (used by widgets on-the-fly)
  static Future<String?> downloadAndCacheOnTheFly(String imageUrl) async {
    if (imageUrl.isEmpty || imageUrl.contains('default-product.png') || kIsWeb) return null;
    try {
      final response = await http.get(Uri.parse(imageUrl)).timeout(const Duration(seconds: 10));
      if (response.statusCode == 200) {
        final base64Image = base64Encode(response.bodyBytes);
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('$_keyPrefix$imageUrl', base64Image);
        return base64Image;
      }
    } catch (_) {}
    return null;
  }
}
