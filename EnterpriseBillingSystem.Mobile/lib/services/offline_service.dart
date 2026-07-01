import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';

class OfflineService {
  static const String _keyCachedProducts = 'cached_products';
  static const String _keyCachedCategories = 'cached_categories';
  static const String _keyCachedCustomers = 'cached_customers';
  static const String _keyCachedCustomerCategories = 'cached_customer_categories';
  static const String _keyCachedPricingProfiles = 'cached_pricing_profiles';
  static const String _keyOfflineCustomers = 'offline_customers';
  static const String _keyOfflineOrders = 'offline_orders';

  // --- Catalog Cache Management ---

  Future<void> cacheProducts(List<dynamic> products) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyCachedProducts, jsonEncode(products));
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
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyCachedCategories, jsonEncode(categories));
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
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyCachedCustomers, jsonEncode(customers));
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
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyCachedCustomerCategories, jsonEncode(categories));
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
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_keyCachedPricingProfiles, jsonEncode(profiles));
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
    await prefs.setStringList(_keyOfflineCustomers, list);

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
      'code': 'TEMP-${customer['tempId'].toString().substring(0, 5).toUpperCase()}',
      'firstName': customer['firstName'],
      'lastName': customer['lastName'],
      'fullName': '${customer['firstName']} ${customer['lastName']}',
      'customerType': customer['customerType'] ?? 'Natural',
      'email': customer['email'],
      'phoneNumber': customer['phoneNumber'],
      'address': customer['address'],
      'municipality': customer['municipality'],
      'city': customer['city'],
      'routeName': 'Ruta Temporal (Offline)',
    });
    await prefs.setString(_keyCachedCustomers, jsonEncode(cachedList));
  }

  Future<List<Map<String, dynamic>>> getOfflineCustomers() async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineCustomers) ?? [];
    return list.map((e) => jsonDecode(e) as Map<String, dynamic>).toList();
  }

  Future<void> saveOfflineCustomers(List<Map<String, dynamic>> customers) async {
    final prefs = await SharedPreferences.getInstance();
    final list = customers.map((e) => jsonEncode(e)).toList();
    await prefs.setStringList(_keyOfflineCustomers, list);
  }

  // --- Offline Order Placement ---

  Future<void> saveOfflineOrder(Map<String, dynamic> order) async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineOrders) ?? [];
    list.add(jsonEncode(order));
    await prefs.setStringList(_keyOfflineOrders, list);
  }

  Future<List<Map<String, dynamic>>> getOfflineOrders() async {
    final prefs = await SharedPreferences.getInstance();
    final list = prefs.getStringList(_keyOfflineOrders) ?? [];
    return list.map((e) => jsonDecode(e) as Map<String, dynamic>).toList();
  }

  Future<void> saveOfflineOrders(List<Map<String, dynamic>> orders) async {
    final prefs = await SharedPreferences.getInstance();
    final list = orders.map((e) => jsonEncode(e)).toList();
    await prefs.setStringList(_keyOfflineOrders, list);
  }

  // --- Helper to check if pending data exists ---

  Future<bool> hasPendingOfflineData() async {
    final prefs = await SharedPreferences.getInstance();
    final customers = prefs.getStringList(_keyOfflineCustomers) ?? [];
    final orders = prefs.getStringList(_keyOfflineOrders) ?? [];
    return customers.isNotEmpty || orders.isNotEmpty;
  }
}
