import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/config_provider.dart';
import '../services/image_cache_service.dart';

class CachedProductImage extends StatefulWidget {
  final String? imageUrl;
  final double? width;
  final double? height;
  final BoxFit fit;
  final double iconSize;

  const CachedProductImage({
    super.key,
    required this.imageUrl,
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

  @override
  void initState() {
    super.initState();
    _loadImage();
  }

  @override
  void didUpdateWidget(covariant CachedProductImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.imageUrl != widget.imageUrl) {
      _loadImage();
    }
  }

  Future<void> _loadImage() async {
    setState(() {
      _isLoading = true;
    });

    var url = widget.imageUrl ?? '';
    if (url.isEmpty || url.contains('default-product.png')) {
      if (mounted) {
        setState(() {
          _filePath = null;
          _isLoading = false;
        });
      }
      return;
    }

    // Convert relative path to absolute URL if necessary
    if (!url.startsWith('http')) {
      try {
        final config = Provider.of<ConfigProvider>(context, listen: false);
        final uri = Uri.parse(config.apiUrl);
        final base = '${uri.scheme}://${uri.host}${uri.hasPort ? ":${uri.port}" : ""}';
        url = '$base${url.startsWith('/') ? "" : "/"}$url';
      } catch (_) {}
    }

    // 1. Check local cache first
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

    // 2. Download and cache on-the-fly
    final downloadedPath = await ImageCacheService.downloadAndCacheOnTheFly(url);
    if (mounted) {
      setState(() {
        _filePath = downloadedPath;
        _isLoading = false;
      });
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
