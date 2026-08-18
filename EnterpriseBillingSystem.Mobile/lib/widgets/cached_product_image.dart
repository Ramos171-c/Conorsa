import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:provider/provider.dart';
import '../providers/config_provider.dart';
import '../services/image_cache_service.dart';

class CachedProductImage extends StatefulWidget {
  final String? imageUrl;
  final String? productCode;
  final double? width;
  final double? height;
  final BoxFit fit;
  final double iconSize;

  const CachedProductImage({
    super.key,
    required this.imageUrl,
    this.productCode,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
    this.iconSize = 50,
  });

  @override
  State<CachedProductImage> createState() => _CachedProductImageState();
}

class _CachedProductImageState extends State<CachedProductImage> {
  String? _filePath;
  bool _isLoading = true;
  bool _isAsset = false;
  String? _assetPath;
  String? _resolvedUrl;

  @override
  void initState() {
    super.initState();
    _loadImage();
  }

  @override
  void didUpdateWidget(covariant CachedProductImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.imageUrl != widget.imageUrl || oldWidget.productCode != widget.productCode) {
      _loadImage();
    }
  }

  Future<bool> _checkAssetExists(String assetPath) async {
    try {
      await rootBundle.load(assetPath);
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> _loadImage() async {
    if (mounted) {
      setState(() {
        _isLoading = true;
        _isAsset = false;
        _assetPath = null;
        _resolvedUrl = null;
      });
    }

    try {
      var url = widget.imageUrl ?? '';

      // 1. If server image URL is provided, prioritize it
      if (url.isNotEmpty && !url.contains('default-product.png')) {
        // Convert relative path to absolute URL if necessary
        if (!url.startsWith('http')) {
          try {
            final config = Provider.of<ConfigProvider>(context, listen: false);
            final uri = Uri.parse(config.apiUrl);
            final base = '${uri.scheme}://${uri.host}${uri.hasPort ? ":${uri.port}" : ""}';
            url = '$base${url.startsWith('/') ? "" : "/"}$url';
          } catch (_) {}
        }

        // Add cache-buster on web so the browser always loads the freshest image
        final displayUrl = kIsWeb
            ? (url.contains('?')
                ? '$url&_v=${DateTime.now().millisecondsSinceEpoch ~/ (1000 * 60 * 60)}'
                : '$url?_v=${DateTime.now().millisecondsSinceEpoch ~/ (1000 * 60 * 60)}')
            : url;

        if (mounted) {
          setState(() {
            _resolvedUrl = displayUrl;
          });
        }

        if (kIsWeb) {
          if (mounted) {
            setState(() {
              _isLoading = false;
            });
          }
          return;
        }

        // On Native: Check local disk cache first
        final cachedPath = ImageCacheService.getCachedImagePath(url);
        if (cachedPath != null) {
          if (mounted) {
            setState(() {
              _filePath = cachedPath;
              _isLoading = false;
            });
          }
          return;
        }

        // Download and cache on-the-fly
        final downloadedPath = await ImageCacheService.downloadAndCacheOnTheFly(url);
        if (downloadedPath != null) {
          if (mounted) {
            setState(() {
              _filePath = downloadedPath;
              _isLoading = false;
            });
          }
          return;
        }
      }

      // 2. Fallback to pre-bundled APK assets if no server image or offline without cache
      if (widget.productCode != null && widget.productCode!.isNotEmpty) {
        final code = widget.productCode!.toUpperCase();
        final formats = ['.png', '.jpg', '.jpeg', '.webp'];
        for (final ext in formats) {
          final path = 'assets/images/$code$ext';
          if (await _checkAssetExists(path)) {
            if (mounted) {
              setState(() {
                _isAsset = true;
                _assetPath = path;
                _isLoading = false;
              });
            }
            return;
          }
        }
      }

      if (mounted) {
        setState(() {
          _filePath = null;
          _isLoading = false;
        });
      }
    } catch (e) {
      debugPrint("Error loading product image: $e");
      if (mounted) {
        setState(() {
          _filePath = null;
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Container(
        width: widget.width,
        height: widget.height,
        color: const Color(0xFFF8FAFC),
        child: const Center(
          child: SizedBox(
            width: 24,
            height: 24,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: Color(0xFF94A3B8),
            ),
          ),
        ),
      );
    }

    // If loaded from pre-packaged APK assets, display immediately
    if (_isAsset && _assetPath != null) {
      return Image.asset(
        _assetPath!,
        width: widget.width,
        height: widget.height,
        fit: widget.fit,
        errorBuilder: (context, error, stackTrace) => _buildPlaceholder(),
      );
    }

    if (kIsWeb) {
      final imgUrl = _resolvedUrl ?? widget.imageUrl ?? '';
      if (imgUrl.isEmpty || imgUrl.contains('default-product.png')) {
        return _buildPlaceholder();
      }
      return Image.network(
        imgUrl,
        width: widget.width,
        height: widget.height,
        fit: widget.fit,
        errorBuilder: (context, error, stackTrace) => _buildPlaceholder(),
      );
    }

    if (_filePath != null && _filePath!.isNotEmpty) {
      try {
        final file = File(_filePath!);
        if (file.existsSync()) {
          return Image.file(
            file,
            width: widget.width,
            height: widget.height,
            fit: widget.fit,
            errorBuilder: (context, error, stackTrace) => _buildPlaceholder(),
          );
        }
      } catch (_) {
        return _buildPlaceholder();
      }
    }

    // Fallback if not loaded, not cached, and no internet
    return _buildPlaceholder();
  }

  Widget _buildPlaceholder() {
    return Container(
      width: widget.width,
      height: widget.height,
      color: const Color(0xFFF8FAFC),
      child: Center(
        child: Icon(
          Icons.restaurant_menu_rounded,
          size: widget.iconSize,
          color: const Color(0xFFCBD5E1),
        ),
      ),
    );
  }
}
