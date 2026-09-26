import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/config_provider.dart';

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
  String? _resolvedUrl;

  @override
  void initState() {
    super.initState();
    _resolveUrl();
  }

  @override
  void didUpdateWidget(covariant CachedProductImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.imageUrl != widget.imageUrl) {
      _resolveUrl();
    }
  }

  void _resolveUrl() {
    var url = widget.imageUrl ?? '';
    if (url.isEmpty || url.contains('default-product.png')) {
      _resolvedUrl = null;
      return;
    }

    try {
      final config = Provider.of<ConfigProvider>(context, listen: false);
      final apiUri = Uri.parse(config.apiUrl);
      final apiBase = '${apiUri.scheme}://${apiUri.host}${apiUri.hasPort ? ":${apiUri.port}" : ""}';

      if (!url.startsWith('http')) {
        url = '$apiBase${url.startsWith('/') ? "" : "/"}$url';
      } else {
        final parsedUrl = Uri.parse(url);
        if (parsedUrl.host != apiUri.host || (apiUri.hasPort && parsedUrl.port != apiUri.port)) {
          url = '$apiBase${parsedUrl.path}${parsedUrl.hasQuery ? "?${parsedUrl.query}" : ""}';
        }
      }
    } catch (_) {}

    if (kIsWeb) {
      final hourBucket = DateTime.now().millisecondsSinceEpoch ~/ (1000 * 60 * 60);
      _resolvedUrl = url.contains('?') ? '$url&_v=$hourBucket' : '$url?_v=$hourBucket';
    } else {
      _resolvedUrl = url;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_resolvedUrl == null || _resolvedUrl!.isEmpty) {
      return _buildPlaceholder();
    }

    return Image.network(
      _resolvedUrl!,
      width: widget.width,
      height: widget.height,
      fit: widget.fit,
      loadingBuilder: (context, child, loadingProgress) {
        if (loadingProgress == null) return child;
        return Container(
          width: widget.width,
          height: widget.height,
          color: const Color(0xFFF8FAFC),
          child: const Center(
            child: SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: Color(0xFF94A3B8),
              ),
            ),
          ),
        );
      },
      errorBuilder: (context, error, stackTrace) => _buildPlaceholder(),
    );
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

