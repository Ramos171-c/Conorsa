import 'dart:convert';
import 'package:flutter/foundation.dart' show kDebugMode;
import 'package:shared_preferences/shared_preferences.dart';

class OfflineService {
  static const String _keyCachedProducts = 'cached_products';
  static const String _keyCachedCategories = 'cached_categories';
  static const String _keyCachedCustomers = 'cached_customers';
  static const String _keyCachedCustomerCategories = 'cached_customer_categories';
  static const String _keyCachedPricingProfiles = 'cached_pricing_profiles';
  static const String _keyOfflineCustomers = 'offline_customers';
  static const String _keyOfflineOrders = 'offline_orders';

  // Helper to purge old image base64 cache keys from SharedPreferences to free up space
  Future<void> _clearOldImageCache() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final keys = prefs.getKeys();
      final keysToRemove = keys.where((k) => k.startsWith('cached_img_')).toList();
      for (final key in keysToRemove) {
        await prefs.remove(key);
      }
      if (kDebugMode) {
        print('OfflineService: Purged ${keysToRemove.length} old cached image keys.');
      }
    } catch (_) {}
  }

  // Safe wrapper for writing String values
  Future<void> _safeSetString(String key, String value) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(key, value);
    } catch (e) {
      if (kDebugMode) {
        print('OfflineService: error writing key $key. Executing emergency purge...');
      }
      await _clearOldImageCache();
      try {
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString(key, value);
      } catch (_) {
        rethrow;
      }
    }
  }

  // Safe wrapper for writing StringList values
  Future<void> _safeSetStringList(String key, List<String> value) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setStringList(key, value);
    } catch (e) {
      if (kDebugMode) {
        print('OfflineService: error writing key list $key. Executing emergency purge...');
      }
      await _clearOldImageCache();
      try {
        final prefs = await SharedPreferences.getInstance();
        await prefs.setStringList(key, value);
      } catch (_) {
        rethrow;
      }
    }
  }

  // --- Catalog Cache Management ---

  Future<void> cacheProducts(List<dynamic> products) async {
    await _safeSetString(_keyCachedProducts, jsonEncode(products));
  }

  Future<List<dynamic>> getCachedProducts() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(_keyCachedProducts);
    if (jsonStr == null) return [];
    try {
      return jsonDecode(jsonStr) as List<dynamic>;
    } catch (_) {
      return [];
    }
  }

  Future<void> cacheCategories(List<dynamic> categories) async {
    await _safeSetString(_keyCachedCategories, jsonEncode(categories));
  }

  Future<List<dynamic>> getCachedCategories() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(_keyCachedCategories);
    if (jsonStr == null) return [];
    try {
      return jsonDecode(jsonStr) as List<dynamic>;
    } catch (_) {
      return [];
    }
  }

  Future<void> cacheCustomers(List<dynamic> customers) async {
    await _safeSetString(_keyCachedCustomers, jsonEncode(customers));
  }

  Future<List<dynamic>> getCachedCustomers() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(_keyCachedCustomers);
    if (jsonStr == null) return [];
    try {
      return jsonDecode(jsonStr) as List<dynamic>;
    } catch (_) {
      return [];
    }
  }

  Future<void> cacheCustomerCategories(List<dynamic> categories) async {
    await _safeSetString(_keyCachedCustomerCategories, jsonEncode(categories));
  }

  Future<List<dynamic>> getCachedCustomerCategories() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(_keyCachedCustomerCategories);
    if (jsonStr == null) return [];
    try {
      return jsonDecode(jsonStr) as List<dynamic>;
    } catch (_) {
      return [];
    }
  }

  Future<void> cachePricingProfiles(List<dynamic> profiles) async {
    await _safeSetString(_keyCachedPricingProfiles, jsonEncode(profiles));
  }

  Future<List<dynamic>> getCachedPricingProfiles() async {
    final prefs = await SharedPreferences.getInstance();
    final jsonStr = prefs.getString(_keyCachedPricingProfiles);
    if (jsonStr == null) return [];
    try {
      return jsonDecode(jsonStr) as List<dynamic>;
    } catch (_) {
      return [];
    }
  }

  // --- Offline Customer Registration ---

  Future<void> saveOfflineCustomer(Map<String, dynamic> customer) async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineCustomers) ?? [];
    list.add(jsonEncode(customer));
    await _safeSetStringList(_keyOfflineCustomers, list);

    // Also add to cached customers list so it's instantly selectable offline
    final cachedStr = prefs.getString(_keyCachedCustomers);
    List<dynamic> cachedList = [];
    if (cachedStr != null) {
      try {
        cachedList = jsonDecode(cachedStr) as List<dynamic>;
      } catch (_) {}
    }
    
    // Add temporary customer entry to cache list
    cachedList.insert(0, {
      'id': customer['tempId'],
      'customerCode': 'TEMP-${customer['tempId'].toString().substring(0, 5).toUpperCase()}',
      'code': 'TEMP-${customer['tempId'].toString().substring(0, 5).toUpperCase()}',
      'name': customer['firstName'],
      'firstName': customer['firstName'],
      'lastName': customer['lastName'] ?? '',
      'fullName': '${customer['firstName']} ${customer['lastName'] ?? ""}'.trim(),
      'customerType': customer['customerType'] ?? 'Natural',
      'email': customer['email'],
      'phoneNumber': customer['phoneNumber'],
      'address': customer['address'],
      'municipality': customer['municipality'],
      'city': customer['city'],
      'routeId': customer['routeId'],
      'routeName': 'Ruta Temporal (Offline)',
      'isTaxExempt': customer['isTaxExempt'] ?? false,
      'defaultDiscountPercentage': customer['defaultDiscountPercentage'] ?? 0.0,
      'status': 1,
      'canUseCredit': customer['canUseCredit'] ?? false,
      'creditLimit': customer['creditLimit'] ?? 0.0,
      'creditDays': customer['creditDays'] ?? 0,
      'customerPricingProfileType': 0,
      'currentDebt': 0.0,
    });
    await _safeSetString(_keyCachedCustomers, jsonEncode(cachedList));
  }

  Future<List<Map<String, dynamic>>> getOfflineCustomers() async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineCustomers) ?? [];
    return list.map((e) => jsonDecode(e) as Map<String, dynamic>).toList();
  }

  Future<void> saveOfflineCustomers(List<Map<String, dynamic>> customers) async {
    final list = customers.map((e) => jsonEncode(e)).toList();
    await _safeSetStringList(_keyOfflineCustomers, list);
  }

  // --- Offline Order Placement ---

  Future<void> saveOfflineOrder(Map<String, dynamic> order) async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineOrders) ?? [];
    list.add(jsonEncode(order));
    await _safeSetStringList(_keyOfflineOrders, list);
  }

  Future<List<Map<String, dynamic>>> getOfflineOrders() async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineOrders) ?? [];
    return list.map((e) => jsonDecode(e) as Map<String, dynamic>).toList();
  }

  Future<void> saveOfflineOrders(List<Map<String, dynamic>> orders) async {
    final list = orders.map((e) => jsonEncode(e)).toList();
    await _safeSetStringList(_keyOfflineOrders, list);
  }

  // --- Helper to check if pending data exists ---

  Future<bool> hasPendingOfflineData() async {
    final prefs = await SharedPreferences.getInstance();
    final customers = prefs.getStringList(_keyOfflineCustomers) ?? [];
    final orders = prefs.getStringList(_keyOfflineOrders) ?? [];
    return customers.isNotEmpty || orders.isNotEmpty;
  }
}
