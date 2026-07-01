import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../providers/config_provider.dart';

class ApiService {
  final ConfigProvider configProvider;
  
  // Callback triggered when token refresh fails and session is cleared
  Function()? onSessionExpired;

  static const String _keyToken = 'access_token';
  static const String _keyRefreshToken = 'refresh_token';
  static const String _keyUsername = 'username';
  static const String _keyTokenExp = 'token_expiration';

  ApiService(this.configProvider);

  String get _baseUrl => configProvider.apiUrl;

  // Retrieve stored token
  Future<String?> getAccessToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyToken);
  }

  // Save auth tokens and info
  Future<void> saveAuthData({
    required String accessToken,
    required String refreshToken,
    required String expiration,
    required String username,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyToken, accessToken);
    await prefs.setString(_keyRefreshToken, refreshToken);
    await prefs.setString(_keyTokenExp, expiration);
    await prefs.setString(_keyUsername, username);
  }

  // Clear auth tokens on logout
  Future<void> clearAuthData() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyToken);
    await prefs.remove(_keyRefreshToken);
    await prefs.remove(_keyTokenExp);
    await prefs.remove(_keyUsername);
  }

  // Check if session exists
  Future<bool> hasSession() async {
    final token = await getAccessToken();
    return token != null && token.isNotEmpty;
  }

  // Base request method with automatic retry on 401 (expired token)
  Future<http.Response> _sendRequest(
    String method,
    String path, {
    Map<String, String>? headers,
    Object? body,
    bool isRetry = false,
  }) async {
    final url = Uri.parse('$_baseUrl$path');
    final token = await getAccessToken();

    final Map<String, String> requestHeaders = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
      ...?headers,
    };

    if (kDebugMode) {
      print('API Request: [$method] $url');
      if (body != null) print('API Body: ${jsonEncode(body)}');
    }

    http.Response response;

    switch (method.toUpperCase()) {
      case 'POST':
        response = await http.post(url, headers: requestHeaders, body: jsonEncode(body));
        break;
      case 'PUT':
        response = await http.put(url, headers: requestHeaders, body: jsonEncode(body));
        break;
      case 'DELETE':
        response = await http.delete(url, headers: requestHeaders, body: jsonEncode(body));
        break;
      case 'GET':
      default:
        response = await http.get(url, headers: requestHeaders);
        break;
    }

    if (kDebugMode) {
      print('API Response: [${response.statusCode}] for $url');
      print('API Response Body: ${response.body}');
    }

    // Handle token expiration / 401 error
    if (response.statusCode == 401 && !isRetry) {
      final refreshed = await _attemptTokenRefresh();
      if (refreshed) {
        // Retry the request once with new token
        return _sendRequest(method, path, headers: headers, body: body, isRetry: true);
      }
    }

    return response;
  }

  // Attempt to refresh JWT token
  Future<bool> _attemptTokenRefresh() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final currentToken = prefs.getString(_keyToken);
      final refreshToken = prefs.getString(_keyRefreshToken);

      if (currentToken == null || refreshToken == null) {
        await clearAuthData();
        return false;
      }

      final url = Uri.parse('$_baseUrl/auth/refresh');
      final response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: jsonEncode({
          'Token': currentToken,
          'RefreshToken': refreshToken,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        await saveAuthData(
          accessToken: data['accessToken'],
          refreshToken: data['refreshToken'],
          expiration: data['expiration'],
          username: data['username'],
        );
        if (kDebugMode) print('API Token refreshed successfully.');
        return true;
      } else {
        if (kDebugMode) print('API Token refresh failed. Status: ${response.statusCode}');
        await clearAuthData();
        onSessionExpired?.call();
        return false;
      }
    } catch (e) {
      if (kDebugMode) print('API Error during token refresh: $e');
      await clearAuthData();
      onSessionExpired?.call();
      return false;
    }
  }

  // Exposed HTTP Methods
  Future<http.Response> get(String path, {Map<String, String>? headers}) =>
      _sendRequest('GET', path, headers: headers);

  Future<http.Response> post(String path, Object body, {Map<String, String>? headers}) =>
      _sendRequest('POST', path, headers: headers, body: body);

  Future<http.Response> put(String path, Object body, {Map<String, String>? headers}) =>
      _sendRequest('PUT', path, headers: headers, body: body);

  Future<http.Response> delete(String path, {Map<String, String>? headers}) =>
      _sendRequest('DELETE', path, headers: headers);
}
