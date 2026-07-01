import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../services/api_service.dart';
import '../models/auth.dart';

class NetworkException implements Exception {}

class AuthProvider extends ChangeNotifier {
  final ApiService apiService;

  bool _isLoggedIn = false;
  bool _isLoading = false;
  String? _errorMessage;
  UserProfile? _userProfile;

  bool get isLoggedIn => _isLoggedIn;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  UserProfile? get userProfile => _userProfile;

  AuthProvider(this.apiService) {
    apiService.onSessionExpired = () {
      _isLoggedIn = false;
      _userProfile = null;
      notifyListeners();
    };
    checkActiveSession();
  }

  // Check if a session already exists and is valid
  Future<void> checkActiveSession() async {
    _isLoading = true;
    notifyListeners();

    try {
      final hasToken = await apiService.hasSession();
      if (hasToken) {
        try {
          // Load the profile to confirm token is valid
          final success = await fetchUserProfile();
          if (success) {
            _isLoggedIn = true;
          } else {
            // Token is invalid/expired and refresh failed
            _isLoggedIn = false;
            await apiService.clearAuthData();
          }
        } on NetworkException {
          // Server unreachable, but we have local token & cached profile.
          // Keep user logged in offline.
          _isLoggedIn = true;
        }
      } else {
        _isLoggedIn = false;
      }
    } catch (e) {
      _isLoggedIn = false;
      _errorMessage = 'Error al verificar sesión: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Log in the user
  Future<bool> login(String username, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await apiService.post('/auth/login', {
        'Username': username,
        'Password': password,
      });

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        
        await apiService.saveAuthData(
          accessToken: data['accessToken'],
          refreshToken: data['refreshToken'],
          expiration: data['expiration'],
          username: data['username'],
        );

        final profileSuccess = await fetchUserProfile();
        if (profileSuccess) {
          _isLoggedIn = true;
          _errorMessage = null;
          return true;
        } else {
          _errorMessage = 'No se pudo cargar el perfil del usuario.';
          await apiService.clearAuthData();
          return false;
        }
      } else {
        final errorData = jsonDecode(response.body);
        _errorMessage = errorData['message'] ?? 'Credenciales incorrectas.';
        return false;
      }
    } catch (e) {
      _errorMessage = 'Error de conexión con el servidor. Verifique la dirección de la API.';
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Fetch current user profile details
  Future<bool> fetchUserProfile() async {
    try {
      final response = await apiService.get('/auth/me');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        _userProfile = UserProfile.fromJson(data);
        
        // Cache user profile for offline session restoration
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('cached_user_profile', response.body);
        return true;
      }
      return false;
    } catch (e) {
      // Offline: Try to load cached user profile
      final prefs = await SharedPreferences.getInstance();
      final cachedData = prefs.getString('cached_user_profile');
      if (cachedData != null) {
        try {
          _userProfile = UserProfile.fromJson(jsonDecode(cachedData));
        } catch (_) {}
      }
      throw NetworkException();
    }
  }

  // Log out the user
  Future<void> logout() async {
    _isLoading = true;
    notifyListeners();

    try {
      final prefs = await SharedPreferences.getInstance();
      final refreshToken = prefs.getString('refresh_token');

      if (refreshToken != null) {
        // Request logout from API
        await apiService.post('/auth/logout', {
          'RefreshToken': refreshToken,
        });
      }
    } catch (_) {
      // Ignore API errors during logout and clear local data anyway
    } finally {
      await apiService.clearAuthData();
      _userProfile = null;
      _isLoggedIn = false;
      _isLoading = false;
      notifyListeners();
    }
  }

  // Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}
