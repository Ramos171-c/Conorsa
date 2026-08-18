import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;

class ImageCacheService {
  static const String _filePrefix = 'cached_img_';

  /// Get a safe local file path for caching an image URL
  static String _getLocalFilePath(String imageUrl) {
    if (kIsWeb) return '';
    // Generate a safe filename using a cleaned version of the URL
    final clean = imageUrl.replaceAll(RegExp(r'[^a-zA-Z0-9]'), '_');
    final filename = clean.length > 100 ? clean.substring(clean.length - 100) : clean;
    return '${Directory.systemTemp.path}/$_filePrefix$filename';
  }

  /// Pre-cache a list of image URLs in the background with limited concurrency
  static Future<void> cacheImages(List<String> imageUrls) async {
    if (kIsWeb) return;
    const maxConcurrent = 3;
    final List<String> urlsToCache = [];
    
    for (final url in imageUrls) {
      if (url.isEmpty || url.contains('default-product.png')) continue;
      if (await isCached(url)) continue;
      urlsToCache.add(url);
    }
    
    if (urlsToCache.isEmpty) return;

    int index = 0;
    Future<void> worker() async {
      while (index < urlsToCache.length) {
        final currentUrl = urlsToCache[index++];
        await _downloadAndCache(currentUrl);
      }
    }

    final List<Future<void>> workers = List.generate(
      urlsToCache.length < maxConcurrent ? urlsToCache.length : maxConcurrent,
      (_) => worker(),
    );

    await Future.wait(workers);
  }

  /// Check if image is cached locally
  static Future<bool> isCached(String imageUrl) async {
    if (kIsWeb) return false;
    if (imageUrl.isEmpty) return false;
    final filePath = _getLocalFilePath(imageUrl);
    final file = File(filePath);
    return file.existsSync();
  }

  /// Get cached image file path
  static String? getCachedImagePath(String imageUrl) {
    if (kIsWeb) return null;
    if (imageUrl.isEmpty) return null;
    final filePath = _getLocalFilePath(imageUrl);
    if (File(filePath).existsSync()) {
      return filePath;
    }
    return null;
  }

  /// Download and cache single image
  static Future<void> _downloadAndCache(String imageUrl) async {
    if (kIsWeb) return;
    try {
      final response = await http.get(Uri.parse(imageUrl)).timeout(const Duration(seconds: 15));
      if (response.statusCode == 200) {
        // Evict any stale cached file for the same product (same UUID, different ticks)
        _evictStaleProductImage(imageUrl);

        final filePath = _getLocalFilePath(imageUrl);
        final file = File(filePath);
        await file.writeAsBytes(response.bodyBytes);
      }
    } catch (_) {
      // Ignore background download errors
    }
  }

  /// Remove any previously cached temp file that belongs to the same product UUID
  static void _evictStaleProductImage(String newUrl) {
    try {
      final uri = Uri.parse(newUrl);
      final segments = uri.pathSegments;
      if (segments.length < 2) return;
      final filename = segments.last; // e.g. "<UUID>_<ticks>.jpg"
      final underscoreIdx = filename.indexOf('_');
      if (underscoreIdx <= 0) return;
      final productId = filename.substring(0, underscoreIdx);

      final tempDir = Directory.systemTemp;
      for (final f in tempDir.listSync()) {
        if (f is File) {
          final name = f.path.split(Platform.pathSeparator).last;
          if (name.startsWith(_filePrefix) &&
              name.contains(productId) &&
              !name.endsWith(filename.replaceAll(RegExp(r'[^a-zA-Z0-9]'), '_'))) {
            f.deleteSync();
          }
        }
      }
    } catch (_) {}
  }

  /// Force cache single image (used by widgets on-the-fly)
  static Future<String?> downloadAndCacheOnTheFly(String imageUrl) async {
    if (kIsWeb) return null;
    if (imageUrl.isEmpty || imageUrl.contains('default-product.png')) return null;
    try {
      final response = await http.get(Uri.parse(imageUrl)).timeout(const Duration(seconds: 10));
      if (response.statusCode == 200) {
        final filePath = _getLocalFilePath(imageUrl);
        final file = File(filePath);
        await file.writeAsBytes(response.bodyBytes);
        return filePath;
      }
    } catch (_) {}
    return null;
  }
}
