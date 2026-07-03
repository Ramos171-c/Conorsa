import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/image_cache_service.dart';

class CachedProductImage extends StatefulWidget {
  final String? imageUrl;
  final double? width;
  final double? height;
  final BoxFit fit;
  final double iconSize;

  const CachedProductImage({
    Key? key,
    required this.imageUrl,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
    this.iconSize = 50,
  }) : super(key: key);

  @override
  State<CachedProductImage> createState() => _CachedProductImageState();
}

class _CachedProductImageState extends State<CachedProductImage> {
  String? _base64Data;
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

    final url = widget.imageUrl ?? '';
    if (url.isEmpty || url.contains('default-product.png')) {
      if (mounted) {
        setState(() {
          _base64Data = null;
          _isLoading = false;
        });
      }
      return;
    }

    // 1. Check local cache first
    final cached = await ImageCacheService.getCachedImageBase64(url);
    if (cached != null) {
      if (mounted) {
        setState(() {
          _base64Data = cached;
          _isLoading = false;
        });
      }
      return;
    }

    // 2. Download and cache on-the-fly
    final downloaded = await ImageCacheService.downloadAndCacheOnTheFly(url);
    if (mounted) {
      setState(() {
        _base64Data = downloaded;
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

    if (_base64Data != null && _base64Data!.isNotEmpty) {
      try {
        final bytes = base64Decode(_base64Data!);
        return Image.memory(
          bytes,
          width: widget.width,
          height: widget.height,
          fit: widget.fit,
          errorBuilder: (context, error, stackTrace) => _buildPlaceholder(),
        );
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
