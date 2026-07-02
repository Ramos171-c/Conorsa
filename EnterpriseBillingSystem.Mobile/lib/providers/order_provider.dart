import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../services/offline_service.dart';
import '../models/customer.dart';
import '../models/product.dart';
import '../models/order.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class OrderProvider extends ChangeNotifier {
  final ApiService apiService;

  List<Customer> _customers = [];
  List<Product> _products = [];
  List<Product> _topProducts = [];
  List<SalesOrderListItem> _orders = [];
  List<dynamic> _productCategories = [];
  double _minimumInvoiceAmount = 600.0;
  
  DraftOrder _draftOrder = DraftOrder(orderDate: DateTime.now(), details: []);
  
  bool _isLoading = false;
  bool _isSyncing = false;
  String? _errorMessage;
  String? _successMessage;

  List<Customer> get customers => _customers;
  List<Product> get products => _products;
  List<Product> get topProducts => _topProducts;
  List<SalesOrderListItem> get orders => _orders;
  List<dynamic> get productCategories => _productCategories;
  double get minimumInvoiceAmount => _minimumInvoiceAmount;
  DraftOrder get draftOrder => _draftOrder;
  bool get isLoading => _isLoading;
  bool get isSyncing => _isSyncing;
  String? get errorMessage => _errorMessage;
  String? get successMessage => _successMessage;

  OrderProvider(this.apiService);

  // Fetch customers with search filter and route filter
  Future<void> fetchCustomers({String? search, String? routeId}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final offlineService = OfflineService();

    try {
      final pageSize = (search == null || search.isEmpty) ? 1000 : 50;
      final searchParam = search != null && search.isNotEmpty ? '&searchTerm=$search' : '';
      final routeParam = routeId != null && routeId.isNotEmpty ? '&routeId=$routeId' : '';
      final response = await apiService.get('/customers?pageNumber=1&pageSize=$pageSize$searchParam$routeParam');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final items = data['items'] as List<dynamic>? ?? [];
        _customers = items.map((e) => Customer.fromJson(e)).toList();
        if (search == null || search.isEmpty) {
          await offlineService.cacheCustomers(items);
        }
        // Connection success: trigger background sync
        syncOfflineData();
      } else {
        _errorMessage = 'No se pudieron cargar los clientes.';
      }
    } catch (e) {
      // Offline fallback
      final cached = await offlineService.getCachedCustomers();
      if (cached.isNotEmpty) {
        var parsed = cached.map((e) => Customer.fromJson(e)).toList();
        if (routeId != null && routeId.isNotEmpty) {
          // Filter offline cached customers by route, keeping CONSUMIDOR FINAL (CUS-000001) or null route
          parsed = parsed.where((c) => 
            c.routeId == routeId || 
            c.routeId == null || 
            c.customerCode == 'CUS-000001'
          ).toList();
        }
        if (search != null && search.isNotEmpty) {
          final query = search.toLowerCase();
          parsed = parsed.where((c) => c.name.toLowerCase().contains(query)).toList();
        }
        _customers = parsed;
      } else {
        _errorMessage = 'Error al cargar clientes: $e';
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Fetch products with search filter
  Future<void> fetchProducts({String? search}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final offlineService = OfflineService();

    try {
      final hasSession = await apiService.hasSession();
      http.Response response;

      if (hasSession) {
        final pageSize = (search == null || search.isEmpty) ? 2000 : 50;
        final searchParam = search != null && search.isNotEmpty ? '&searchTerm=$search' : '';
        response = await apiService.get('/products?pageNumber=1&pageSize=$pageSize$searchParam');
      } else {
        response = await apiService.get('/catalog/products');
      }

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final List<dynamic> items = data is Map ? (data['items'] as List<dynamic>? ?? []) : (data as List<dynamic>);
        
        var parsedProducts = items
            .map((e) => Product.fromJson(e))
            .where((p) => p.isActive)
            .toList();

        // Apply in-memory search filter for the guest catalog endpoint
        if (!hasSession && search != null && search.isNotEmpty) {
          final query = search.toLowerCase();
          parsedProducts = parsedProducts.where((p) =>
              p.name.toLowerCase().contains(query) ||
              p.internalCode.toLowerCase().contains(query)
          ).toList();
        }

        _products = parsedProducts;
        if (search == null || search.isEmpty) {
          await offlineService.cacheProducts(items);
        }
        // Connection success: trigger background sync
        syncOfflineData();
      } else {
        _errorMessage = 'No se pudieron cargar los productos (Status: ${response.statusCode}, Body: ${response.body}).';
      }
    } catch (e) {
      // Offline fallback
      final cached = await offlineService.getCachedProducts();
      if (cached.isNotEmpty) {
        var parsed = cached
            .map((e) => Product.fromJson(e))
            .where((p) => p.isActive)
            .toList();
        if (search != null && search.isNotEmpty) {
          final query = search.toLowerCase();
          parsed = parsed.where((p) =>
              p.name.toLowerCase().contains(query) ||
              p.internalCode.toLowerCase().contains(query)
          ).toList();
        }
        _products = parsed;
      } else {
        _errorMessage = 'Error al cargar productos: $e';
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Fetch placed orders history
  Future<void> fetchOrders() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await apiService.get('/sales-orders?pageNumber=1&pageSize=50');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final items = data['items'] as List<dynamic>? ?? [];
        _orders = items.map((e) => SalesOrderListItem.fromJson(e)).toList();
        // Connection success: trigger background sync
        syncOfflineData();
      } else {
        _errorMessage = 'No se pudieron cargar los pedidos.';
      }
    } catch (e) {
      // For orders, if offline we just keep what was loaded, or show connection error
      _errorMessage = 'Error al cargar pedidos (Modo sin conexión): $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Set selected customer for the draft order
  void setCustomer(Customer customer) {
    if (customer.isBlocked || customer.isInactive) {
      _errorMessage = 'No se puede seleccionar un cliente bloqueado o inactivo.';
      notifyListeners();
      return;
    }
    _draftOrder.customerId = customer.id;
    _draftOrder.customerName = customer.name;
    _draftOrder.isCustomerTaxExempt = customer.isTaxExempt;
    _draftOrder.customerDefaultDiscount = customer.defaultDiscountPercentage;

    // Apply customer discount to existing cart details if any
    if (_draftOrder.details.isNotEmpty) {
      final updatedDetails = _draftOrder.details.map((item) {
        return DraftOrderDetail(
          productId: item.productId,
          productName: item.productName,
          productCode: item.productCode,
          unitOfMeasureId: item.unitOfMeasureId,
          unitOfMeasureCode: item.unitOfMeasureCode,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          taxPercentage: item.taxPercentage,
          discountPercentage: customer.defaultDiscountPercentage,
        );
      }).toList();
      _draftOrder.details = updatedDetails;
    }

    _errorMessage = null;
    notifyListeners();
  }

  // Add item to draft order (cart)
  void addToCart(Product product, ProductPresentation presentation, double quantity) {
    if (_draftOrder.customerId == null) {
      _errorMessage = 'Debe seleccionar un cliente antes de agregar productos.';
      notifyListeners();
      return;
    }

    // Check if item already in cart
    final index = _draftOrder.details.indexWhere(
      (item) => item.productId == product.id && item.unitOfMeasureId == presentation.unitOfMeasureId,
    );

    if (index >= 0) {
      // Update existing item quantity
      final existingItem = _draftOrder.details[index];
      final newQty = existingItem.quantity + quantity;
      updateQuantity(product.id, presentation.unitOfMeasureId, newQty);
    } else {
      // Add new item
      _draftOrder.details.add(DraftOrderDetail(
        productId: product.id,
        productName: product.name,
        productCode: product.internalCode,
        unitOfMeasureId: presentation.unitOfMeasureId,
        unitOfMeasureCode: presentation.unitOfMeasureCode,
        quantity: quantity,
        unitPrice: presentation.retailPrice > 0 ? presentation.retailPrice : product.defaultSalePrice,
        taxPercentage: presentation.taxPercentage,
        discountPercentage: _draftOrder.customerDefaultDiscount,
      ));
      _errorMessage = null;
      notifyListeners();
    }
  }

  // Update item quantity
  void updateQuantity(String productId, String unitOfMeasureId, double quantity) {
    final index = _draftOrder.details.indexWhere(
      (item) => item.productId == productId && item.unitOfMeasureId == unitOfMeasureId,
    );

    if (index >= 0) {
      if (quantity <= 0) {
        _draftOrder.details.removeAt(index);
      } else {
        final item = _draftOrder.details[index];
        _draftOrder.details[index] = DraftOrderDetail(
          productId: item.productId,
          productName: item.productName,
          productCode: item.productCode,
          unitOfMeasureId: item.unitOfMeasureId,
          unitOfMeasureCode: item.unitOfMeasureCode,
          quantity: quantity,
          unitPrice: item.unitPrice,
          taxPercentage: item.taxPercentage,
          discountPercentage: item.discountPercentage,
        );
      }
      _errorMessage = null;
      notifyListeners();
    }
  }

  // Remove item from cart
  void removeFromCart(String productId, String unitOfMeasureId) {
    _draftOrder.details.removeWhere(
      (item) => item.productId == productId && item.unitOfMeasureId == unitOfMeasureId,
    );
    notifyListeners();
  }

  // Set notes
  void setNotes(String val) {
    _draftOrder.notes = val;
  }

  // Reset/Clear cart
  void clearDraft() {
    _draftOrder = DraftOrder(
      orderDate: DateTime.now(),
      details: [],
    );
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();
  }

  // Submit draft order to Web API
  Future<bool> submitOrder() async {
    if (_draftOrder.customerId == null) {
      _errorMessage = 'Debe seleccionar un cliente.';
      notifyListeners();
      return false;
    }
    if (_draftOrder.details.isEmpty) {
      _errorMessage = 'El pedido debe tener al menos un producto.';
      notifyListeners();
      return false;
    }
    if (_draftOrder.totalAmount < _minimumInvoiceAmount) {
      _errorMessage = 'El monto total del pedido de venta debe ser igual o mayor a C\$${_minimumInvoiceAmount.toStringAsFixed(2)}.';
      notifyListeners();
      return false;
    }

    _isLoading = true;
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();

    final body = {
      'CustomerId': _draftOrder.customerId,
      'OrderDate': _draftOrder.orderDate.toIso8601String(),
      'Notes': _draftOrder.notes,
      'Details': _draftOrder.details.map((d) => {
        'ProductId': d.productId,
        'UnitOfMeasureId': d.unitOfMeasureId,
        'Quantity': d.quantity,
        'UnitPrice': d.unitPrice,
        'DiscountPercentage': d.discountPercentage,
        'TaxPercentage': d.taxPercentage,
      }).toList(),
    };

    try {
      final response = await apiService.post('/sales-orders', body);

      if (response.statusCode == 201 || response.statusCode == 200) {
        _successMessage = 'Pedido creado correctamente.';
        clearDraft();
        fetchOrders(); // Refresh order history
        return true;
      } else {
        try {
          final errorData = jsonDecode(response.body);
          _errorMessage = errorData['detail'] ?? errorData['message'] ?? errorData['title'] ?? 'Error al guardar el pedido en el servidor.';
        } catch (_) {
          _errorMessage = 'Error del servidor: ${response.statusCode}';
        }
        return false;
      }
    } catch (e) {
      // Save offline if connection fails
      try {
        final offlineService = OfflineService();
        final offlineOrder = {
          'tempId': 'temp_order_${DateTime.now().millisecondsSinceEpoch}',
          'CustomerId': _draftOrder.customerId,
          'OrderDate': _draftOrder.orderDate.toIso8601String(),
          'Notes': _draftOrder.notes,
          'Details': _draftOrder.details.map((d) => {
            'ProductId': d.productId,
            'UnitOfMeasureId': d.unitOfMeasureId,
            'Quantity': d.quantity,
            'UnitPrice': d.unitPrice,
            'DiscountPercentage': d.discountPercentage,
            'TaxPercentage': d.taxPercentage,
          }).toList(),
        };

        await offlineService.saveOfflineOrder(offlineOrder);
        _successMessage = 'Guardado localmente. Pedido creado de forma local por falta de conexión a internet.';
        clearDraft();
        return true;
      } catch (saveError) {
        _errorMessage = 'Error de conexión y fallo al guardar localmente: $saveError';
        return false;
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void clearMessages() {
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();
  }

  // Fetch top 10 products for a specific customer
  Future<void> fetchCustomerTopProducts(String customerId) async {
    _isLoading = true;
    _errorMessage = null;
    _topProducts = [];
    notifyListeners();

    try {
      final response = await apiService.get('/customers/$customerId/top-products?limit=10');
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        _topProducts = data.map((e) => Product.fromJson(e)).toList();
      } else {
        _errorMessage = 'No se pudieron cargar los productos frecuentes.';
      }
    } catch (e) {
      _errorMessage = 'Error al cargar productos frecuentes: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Fetch product categories
  Future<void> fetchProductCategories() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    final offlineService = OfflineService();

    try {
      final response = await apiService.get('/categories?pageNumber=1&pageSize=100');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        _productCategories = data['items'] as List<dynamic>? ?? [];
        await offlineService.cacheCategories(_productCategories);
      } else {
        _errorMessage = 'No se pudieron cargar las categorías de productos.';
      }
    } catch (e) {
      // Offline fallback
      _productCategories = await offlineService.getCachedCategories();
      if (_productCategories.isEmpty) {
        _errorMessage = 'Error al cargar categorías de productos: $e';
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // Fetch system parameters
  Future<void> fetchSystemParameters() async {
    try {
      final response = await apiService.get('/system/parameters');
      if (response.statusCode == 200) {
        final Map<String, dynamic> params = jsonDecode(response.body);
        if (params.containsKey('MinimumInvoiceAmount')) {
          _minimumInvoiceAmount = double.tryParse(params['MinimumInvoiceAmount'].toString()) ?? 600.0;
          notifyListeners();
        }
      }
    } catch (_) {
      // Keep fallback
    }
  }

  // Fetch single order details
  Future<SalesOrderDetail?> fetchOrderDetail(String orderId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await apiService.get('/sales-orders/$orderId');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return SalesOrderDetail.fromJson(data);
      } else {
        _errorMessage = 'Error al cargar el detalle del pedido (Código ${response.statusCode}).';
      }
    } catch (e) {
      _errorMessage = 'Error al cargar detalle del pedido: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return null;
  }

  // Cancel order (solicitar anulación)
  Future<bool> cancelOrder(String orderId, String reason) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await apiService.post('/sales-orders/$orderId/request-cancellation', {
        'SalesOrderId': orderId,
        'Reason': reason,
      });

      if (response.statusCode == 200 || response.statusCode == 204) {
        await fetchOrders(); // Refresh order list
        return true;
      } else {
        try {
          final err = jsonDecode(response.body);
          _errorMessage = err['detail'] ?? err['message'] ?? 'Error al solicitar la anulación del pedido.';
        } catch (_) {
          _errorMessage = 'Error al solicitar la anulación del pedido (Código ${response.statusCode}).';
        }
      }
    } catch (e) {
      _errorMessage = 'Error de conexión: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  // Background Automatic Synchronization of Offline Registrations and Orders
  Future<void> syncOfflineData() async {
    if (_isSyncing) return;

    final offlineService = OfflineService();
    final hasData = await offlineService.hasPendingOfflineData();
    if (!hasData) return;

    _isSyncing = true;
    notifyListeners();

    try {
      // 1. Sync offline registered customers first
      final offlineCustomers = await offlineService.getOfflineCustomers();
      final Map<String, String> tempIdToRealIdMap = {};

      if (offlineCustomers.isNotEmpty) {
        final List<Map<String, dynamic>> remainingCustomers = [];
        for (var cust in offlineCustomers) {
          try {
            final body = {
              'IdentificationNumber': cust['identificationNumber'],
              'IdentificationType': cust['identificationType'],
              'CustomerType': cust['customerType'],
              'Name': cust['firstName'], // backend expects 'Name'
              'LegalName': cust['legalName'],
              'CustomerCategoryId': cust['customerCategoryId'],
              'CustomerPricingProfileId': cust['customerPricingProfileId'],
              'CreditLimit': cust['creditLimit'],
              'CreditDays': cust['creditDays'],
              'CanUseCredit': cust['canUseCredit'],
              'IsTaxExempt': cust['isTaxExempt'],
              'DefaultDiscountPercentage': cust['defaultDiscountPercentage'],
              'Addresses': cust['addresses'],
              'Phones': cust['phones'],
              'Emails': cust['emails'],
              'Contacts': cust['contacts'],
            };

            final response = await apiService.post('/customers', body);
            if (response.statusCode == 201 || response.statusCode == 200) {
              final data = jsonDecode(response.body);
              String realId;
              if (data is Map) {
                realId = (data['id'] ?? data['Id'] ?? response.body).toString().replaceAll('"', '');
              } else {
                realId = data.toString().replaceAll('"', '');
              }
              tempIdToRealIdMap[cust['tempId']] = realId;
              print('Sync: Customer synced successfully! RealId: $realId');
            } else {
              print('Sync: Failed to sync customer. Status: ${response.statusCode}, Body: ${response.body}');
              remainingCustomers.add(cust);
            }
          } catch (e) {
            print('Sync: Customer exception: $e');
            remainingCustomers.add(cust);
          }
        }
        await offlineService.saveOfflineCustomers(remainingCustomers);
      }

      // 2. Sync offline placed orders
      final offlineOrders = await offlineService.getOfflineOrders();
      if (offlineOrders.isNotEmpty) {
        final List<Map<String, dynamic>> remainingOrders = [];
        for (var order in offlineOrders) {
          try {
            var customerId = order['CustomerId'] as String;
            if (tempIdToRealIdMap.containsKey(customerId)) {
              customerId = tempIdToRealIdMap[customerId]!;
            }

            // Skip syncing this order if it references a temporary customer that failed to sync
            if (customerId.startsWith('temp_')) {
              print('Sync: Skipping order sync because customer registration failed.');
              remainingOrders.add(order);
              continue;
            }

            final body = {
              'CustomerId': customerId,
              'OrderDate': order['OrderDate'],
              'Notes': order['Notes'],
              'Details': order['Details'],
            };

            final response = await apiService.post('/sales-orders', body);
            if (response.statusCode == 201 || response.statusCode == 200) {
              print('Sync: Order synced successfully!');
            } else {
              print('Sync: Failed to sync order. Status: ${response.statusCode}, Body: ${response.body}');
              remainingOrders.add(order);
            }
          } catch (e) {
            print('Sync: Order exception: $e');
            remainingOrders.add(order);
          }
        }
        await offlineService.saveOfflineOrders(remainingOrders);
      }

      // Retrieve the routeId from cached user profile for background sync
      String? routeId;
      try {
        final prefs = await SharedPreferences.getInstance();
        final cachedData = prefs.getString('cached_user_profile');
        if (cachedData != null) {
          final profileMap = jsonDecode(cachedData);
          routeId = profileMap['routeId'] as String?;
        }
      } catch (_) {}

      final routeParam = routeId != null && routeId.isNotEmpty ? '&routeId=$routeId' : '';
      // Refresh listings and cache once sync is complete
      final responseCust = await apiService.get('/customers?pageNumber=1&pageSize=1000$routeParam');
      if (responseCust.statusCode == 200) {
        final data = jsonDecode(responseCust.body);
        final items = data['items'] as List<dynamic>? ?? [];
        _customers = items.map((e) => Customer.fromJson(e)).toList();
        await offlineService.cacheCustomers(items);
      }

      final responseProd = await apiService.get('/products?pageNumber=1&pageSize=2000');
      if (responseProd.statusCode == 200) {
        final data = jsonDecode(responseProd.body);
        final List<dynamic> items = data['items'] as List<dynamic>? ?? [];
        _products = items.map((e) => Product.fromJson(e)).where((p) => p.isActive).toList();
        await offlineService.cacheProducts(items);
      }

      final responseCat = await apiService.get('/categories?pageNumber=1&pageSize=100');
      if (responseCat.statusCode == 200) {
        final data = jsonDecode(responseCat.body);
        _productCategories = data['items'] as List<dynamic>? ?? [];
        await offlineService.cacheCategories(_productCategories);
      }

      final responseOrders = await apiService.get('/sales-orders?pageNumber=1&pageSize=50');
      if (responseOrders.statusCode == 200) {
        final data = jsonDecode(responseOrders.body);
        final items = data['items'] as List<dynamic>? ?? [];
        _orders = items.map((e) => SalesOrderListItem.fromJson(e)).toList();
      }
    } catch (e, stack) {
      print('Sync: Outer sync exception: $e');
      print('Sync: StackTrace: $stack');
      // Ignore network drops during background synchronization
    } finally {
      _isSyncing = false;
      notifyListeners();
    }
  }
}
