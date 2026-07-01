import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ConfigProvider extends ChangeNotifier {
  static const String _keyApiUrl = 'api_base_url';
  static const String defaultUrl = 'http://167.99.13.177:8080/api/v1';

  String _apiUrl = defaultUrl;
  bool _isInitialized = false;

  String get apiUrl => _apiUrl;
  bool get isInitialized => _isInitialized;

  ConfigProvider();

  Future<void> loadConfig() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      _apiUrl = prefs.getString(_keyApiUrl) ?? defaultUrl;
    } catch (e) {
      _apiUrl = defaultUrl;
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
