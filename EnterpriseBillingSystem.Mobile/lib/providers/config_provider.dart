import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ConfigProvider extends ChangeNotifier {
  static const String _keyApiUrl = 'api_base_url';
  static const String defaultUrl = 'http://167.99.13.177:8081/api/v1';

  String _apiUrl = defaultUrl;
  bool _isInitialized = false;

  String get apiUrl => _apiUrl;
  bool get isInitialized => _isInitialized;

  ConfigProvider();

  static String get _dynamicDefaultUrl {
    if (kIsWeb) {
      try {
        final base = Uri.base;
        final portPart = base.hasPort && base.port != 80 && base.port != 443
            ? ':${base.port}'
            : '';
        return '${base.scheme}://${base.host}$portPart/api/v1';
      } catch (_) {}
    }
    return defaultUrl;
  }

  Future<void> loadConfig() async {
    final fallbackUrl = _dynamicDefaultUrl;
    try {
      if (kIsWeb) {
        // On Web, always prioritize the active origin to prevent CORS or mismatched host IP issues
        _apiUrl = fallbackUrl;
      } else {
        final prefs = await SharedPreferences.getInstance();
        _apiUrl = prefs.getString(_keyApiUrl) ?? fallbackUrl;
      }
    } catch (e) {
      _apiUrl = fallbackUrl;
    } finally {
      _isInitialized = true;
      notifyListeners();
    }
  }

  Future<void> updateApiUrl(String newUrl) async {
    // Normalize URL
    var formattedUrl = newUrl.trim();
    if (formattedUrl.endsWith('/')) {
      formattedUrl = formattedUrl.substring(0, formattedUrl.length - 1);
    }
    if (!formattedUrl.endsWith('/api/v1')) {
      if (formattedUrl.endsWith('/api')) {
        formattedUrl = '$formattedUrl/v1';
      } else {
        formattedUrl = '$formattedUrl/api/v1';
      }
    }

    _apiUrl = formattedUrl;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyApiUrl, _apiUrl);
    notifyListeners();
  }
}
