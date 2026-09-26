import 'package:shared_preferences/shared_preferences.dart';

class ImageCacheService {
  static const String _keyPrefix = 'cached_img_';

  /// Purges all legacy base64 image strings from SharedPreferences
  /// to prevent Android TransactionTooLargeException and startup crashes.
  static Future<void> purgeLegacyCache() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final keys = prefs.getKeys().where((k) => k.startsWith(_keyPrefix)).toList();
      for (final key in keys) {
        await prefs.remove(key);
      }
    } catch (_) {}
  }

  /// No-op background pre-cache since images are natively cached in memory by Flutter Image.network
  static Future<void> cacheImages(List<String> imageUrls) async {
    // Purge any stale legacy keys if present
    await purgeLegacyCache();
  }

  /// Legacy helper - always returns false to use Flutter's native network image loader
  static Future<bool> isCached(String imageUrl) async {
    return false;
  }

  /// Legacy helper
  static Future<String?> getCachedImageBase64(String imageUrl) async {
    return null;
  }

  /// Legacy helper
  static Future<String?> downloadAndCacheOnTheFly(String imageUrl) async {
    return null;
  }
}

