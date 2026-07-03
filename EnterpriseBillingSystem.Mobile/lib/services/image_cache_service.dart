import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ImageCacheService {
  static const String _keyPrefix = 'cached_img_';

  /// Pre-cache a list of image URLs in the background
  static Future<void> cacheImages(List<String> imageUrls) async {
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
    final prefs = await SharedPreferences.getInstance();
    return prefs.containsKey('$_keyPrefix$imageUrl');
  }

  /// Get cached image bytes
  static Future<String?> getCachedImageBase64(String imageUrl) async {
    if (imageUrl.isEmpty) return null;
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('$_keyPrefix$imageUrl');
  }

  /// Download and cache single image
  static Future<void> _downloadAndCache(String imageUrl) async {
    try {
      final response = await http.get(Uri.parse(imageUrl)).timeout(const Duration(seconds: 15));
      if (response.statusCode == 200) {
        final base64Image = base64Encode(response.bodyBytes);
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('$_keyPrefix$imageUrl', base64Image);
      }
    } catch (_) {
      // Ignore background download errors
    }
  }

  /// Force cache single image (used by widgets on-the-fly)
  static Future<String?> downloadAndCacheOnTheFly(String imageUrl) async {
    if (imageUrl.isEmpty || imageUrl.contains('default-product.png')) return null;
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
