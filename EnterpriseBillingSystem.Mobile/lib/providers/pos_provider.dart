import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../models/product.dart';
import '../models/customer.dart';
import '../models/cash_session.dart';
import '../models/order.dart';

class CartItem {
  final Product product;
  ProductPresentation presentation;
  double quantity;
  double unitPriceDisplayed;
  double lineTotal;

  CartItem({
    required this.product,
    required this.presentation,
    required this.quantity,
    required this.unitPriceDisplayed,
    required this.lineTotal,
  });
}

class PosProvider extends ChangeNotifier {
  final ApiService apiService;

  final List<CartItem> _cart = [];
  Customer? _selectedCustomer;
  CashSession? _cashSession;
  String? _editingOrderId;

  double _subtotalBase = 0.0;
  double _subtotalCommercial = 0.0;
  double _exchangeRate = 36.5;

  String _currentLevel = 'DETALLE';
  String _manualPricingLevelOverride = 'DETALLE';
  String _nextLevel = 'SEMI MAYORISTA';
  double _progressToNextLevel = 0.0;
  double _missingToNextLevel = 0.0;

  bool _isBusy = false;
  String? _errorMessage;
  String? _successMessage;

  List<dynamic> _currencies = [];
  double _semiWholesaleThreshold = 10000.0;
  double _wholesaleThreshold = 30000.0;
  double _minimumInvoiceAmount = 600.0;

  // Getters
  List<CartItem> get cart => _cart;
  Customer? get selectedCustomer => _selectedCustomer;
  CashSession? get cashSession => _cashSession;
  String? get editingOrderId => _editingOrderId;
  double get subtotalBase => _subtotalBase;
  double get subtotalCommercial => _subtotalCommercial;
  double get exchangeRate => _exchangeRate;
  double get usdTotal => _subtotalCommercial / _exchangeRate;

  String get currentLevel => _currentLevel;
  String get manualPricingLevelOverride => _manualPricingLevelOverride;
  String get nextLevel => _nextLevel;
  double get progressToNextLevel => _progressToNextLevel;
  double get missingToNextLevel => _missingToNextLevel;

  bool get isBusy => _isBusy;
  String? get errorMessage => _errorMessage;
  String? get successMessage => _successMessage;

  List<dynamic> get currencies => _currencies;
  double get semiWholesaleThreshold => _semiWholesaleThreshold;
  double get wholesaleThreshold => _wholesaleThreshold;
  double get minimumInvoiceAmount => _minimumInvoiceAmount;

  PosProvider(this.apiService);

  // Set selected customer
  void setCustomer(Customer? customer) {
    _selectedCustomer = customer;
    _manualPricingLevelOverride = 'DETALLE';
    _errorMessage = null;
    recalculatePricing();
  }

  // Set manual pricing level override
  void setManualPricingLevelOverride(String level) {
    _manualPricingLevelOverride = level;
    recalculatePricing();
  }

  // Set exchange rate
  void setExchangeRate(double rate) {
    if (rate > 0) {
      _exchangeRate = rate;
      notifyListeners();
    }
  }

  // Clear messages
  void clearMessages() {
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();
  }

  // Fetch active currencies and pricing thresholds dynamically from API
  Future<void> fetchConfigurations() async {
    _isBusy = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // 1. Fetch Thresholds
      final thresholdResponse = await apiService.get('/pricing-thresholds');
      if (thresholdResponse.statusCode == 200) {
        final List<dynamic> data = jsonDecode(thresholdResponse.body);
        for (var item in data) {
          final String levelName = (item['levelName'] as String).toUpperCase();
          final double limit = (item['minimumSubtotal'] as num).toDouble();
          if (levelName.contains('SEMI')) {
            _semiWholesaleThreshold = limit;
          } else if (levelName.contains('MAYORISTA') || levelName.contains('WHOLESALE')) {
            _wholesaleThreshold = limit;
          }
        }
      }

      // 2. Fetch System Parameters
      final paramResponse = await apiService.get('/system/parameters');
      if (paramResponse.statusCode == 200) {
        final Map<String, dynamic> params = jsonDecode(paramResponse.body);
        if (params.containsKey('MinimumInvoiceAmount')) {
          _minimumInvoiceAmount = double.tryParse(params['MinimumInvoiceAmount'].toString()) ?? 600.0;
        }
      }
      final currencyResponse = await apiService.get('/currencies');
      if (currencyResponse.statusCode == 200) {
        _currencies = jsonDecode(currencyResponse.body);
        // Find default or USD exchange rate
        final usd = _currencies.firstWhere(
          (c) => (c['code'] as String).toUpperCase() == 'USD',
          orElse: () => null,
        );
        if (usd != null) {
          _exchangeRate = (usd['exchangeRate'] as num).toDouble();
        }
      }
      
      recalculatePricing();
    } catch (e) {
      // Keep fallbacks in case of network errors
    } finally {
      _isBusy = false;
      notifyListeners();
    }
  }

  // Fetch active cash session
  Future<void> fetchActiveCashSession(String username) async {
    _isBusy = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await apiService.get('/cash-sessions?status=Open&pageSize=100');
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final items = data['items'] as List<dynamic>? ?? [];
        
        final userSessionJson = items.firstWhere(
          (s) => (s['openedByUserName'] as String).toLowerCase() == username.toLowerCase(),
          orElse: () => null,
        );

        if (userSessionJson != null) {
          _cashSession = CashSession.fromJson(userSessionJson);
        } else {
          _cashSession = null;
          _errorMessage = 'No se encontró una sesión de caja abierta para este usuario.';
        }
      } else {
        _errorMessage = 'Error al consultar sesiones de caja.';
      }
    } catch (e) {
      _errorMessage = 'Error de conexión: $e';
    } finally {
      _isBusy = false;
      notifyListeners();
    }
  }



  // Add item to cart
  void addToCart(Product product, ProductPresentation presentation, double quantity) {
    _errorMessage = null;

    final index = _cart.indexWhere(
      (item) => item.product.id == product.id && item.presentation.id == presentation.id,
    );

    if (index >= 0) {
      _cart[index].quantity += quantity;
    } else {
      _cart.add(CartItem(
        product: product,
        presentation: presentation,
        quantity: quantity,
        unitPriceDisplayed: presentation.retailPrice,
        lineTotal: presentation.retailPrice * quantity,
      ));
    }

    recalculatePricing();
  }

  // Update item quantity
  void updateQuantity(CartItem item, double quantity) {
    _errorMessage = null;
    final index = _cart.indexOf(item);
    if (index >= 0) {
      if (quantity <= 0) {
        _cart.removeAt(index);
      } else {
        _cart[index].quantity = quantity;
      }
      recalculatePricing();
    }
  }

  // Remove from cart
  void removeFromCart(CartItem item) {
    _cart.remove(item);
    recalculatePricing();
  }

  // Clear cart
  void clearCart() {
    _cart.clear();
    _subtotalBase = 0.0;
    _subtotalCommercial = 0.0;
    _currentLevel = 'DETALLE';
    _manualPricingLevelOverride = 'DETALLE';
    _nextLevel = 'SEMI MAYORISTA';
    _progressToNextLevel = 0.0;
    _missingToNextLevel = 0.0;
    _errorMessage = null;
    _successMessage = null;
    _editingOrderId = null;
    notifyListeners();
  }

  // Motor de Precios Dinámicos
  void recalculatePricing() {
    // 1. Calculate Subtotal Base (always quantity * retailPrice)
    _subtotalBase = 0.0;
    for (var item in _cart) {
      _subtotalBase += item.quantity * item.presentation.retailPrice;
    }

    // 2. Determine Cart Level using manual override (defaults to 'DETALLE')

    // 3. Determine Level using manual override (defaults to 'DETALLE')
    int finalLevel = 0;
    if (_manualPricingLevelOverride == 'MAYORISTA') {
      finalLevel = 2;
    } else if (_manualPricingLevelOverride == 'SEMI MAYORISTA') {
      finalLevel = 1;
    }

    // 5. Map final level and update unitPriceDisplayed/lineTotal on all items
    _subtotalCommercial = 0.0;
    _currentLevel = finalLevel == 2 
        ? 'MAYORISTA' 
        : (finalLevel == 1 ? 'SEMI MAYORISTA' : 'DETALLE');

    for (var item in _cart) {
      double price = item.presentation.retailPrice;
      if (finalLevel == 2) {
        price = item.presentation.wholesalePrice > 0 
            ? item.presentation.wholesalePrice 
            : item.presentation.retailPrice;
      } else if (finalLevel == 1) {
        price = item.presentation.semiWholesalePrice > 0 
            ? item.presentation.semiWholesalePrice 
            : item.presentation.retailPrice;
      }

      item.unitPriceDisplayed = price;
      item.lineTotal = price * item.quantity;
      _subtotalCommercial += item.lineTotal;
    }

    // 6. Update progress to next level
    if (finalLevel == 0) {
      _nextLevel = 'SEMI MAYORISTA';
      _missingToNextLevel = (_semiWholesaleThreshold + 1.0) - _subtotalBase;
      _progressToNextLevel = (_subtotalBase / (_semiWholesaleThreshold + 1.0)).clamp(0.0, 1.0);
    } else if (finalLevel == 1) {
      _nextLevel = 'MAYORISTA';
      _missingToNextLevel = (_wholesaleThreshold + 1.0) - _subtotalBase;
      _progressToNextLevel = (_subtotalBase / (_wholesaleThreshold + 1.0)).clamp(0.0, 1.0);
    } else {
      _nextLevel = '';
      _missingToNextLevel = 0.0;
      _progressToNextLevel = 1.0;
    }

    notifyListeners();
  }

  // Checkout (Crear Pedido / Sales Order)
  Future<bool> checkout({
    String? notes,
  }) async {
    _isBusy = true;
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();

    try {
      // Validations
      if (_cart.isEmpty) {
        _errorMessage = 'El carrito está vacío.';
        return false;
      }

      final customerId = _selectedCustomer?.id ?? '00000000-0000-0000-0000-000000000000';
      if (customerId == '00000000-0000-0000-0000-000000000000') {
        _errorMessage = 'Debe seleccionar un cliente para procesar el pedido.';
        return false;
      }

      if (_subtotalCommercial < _minimumInvoiceAmount) {
        _errorMessage = 'El monto total del pedido de venta debe ser igual o mayor a C\$${_minimumInvoiceAmount.toStringAsFixed(2)}.';
        return false;
      }

      if (_editingOrderId != null) {
        // Update existing order
        final updateBody = {
          'Id': _editingOrderId,
          'CustomerId': customerId,
          'OrderDate': DateTime.now().toUtc().toIso8601String(),
          'Notes': notes ?? 'Pedido Modificado desde POS Móvil (Vendedor)',
          'Details': _cart.map((item) {
            return {
              'ProductId': item.product.id,
              'UnitOfMeasureId': item.presentation.unitOfMeasureId,
              'Quantity': item.quantity,
              'UnitPrice': item.unitPriceDisplayed,
              'DiscountPercentage': 0.0,
              'TaxPercentage': item.presentation.taxPercentage,
            };
          }).toList(),
        };

        final response = await apiService.put('/sales-orders/$_editingOrderId', updateBody);
        if (response.statusCode != 200 && response.statusCode != 204) {
          try {
            final err = jsonDecode(response.body);
            _errorMessage = err['detail'] ?? err['message'] ?? err['title'] ?? 'Error al modificar el pedido (Código ${response.statusCode}).';
          } catch (_) {
            _errorMessage = 'Error al modificar el pedido (Código ${response.statusCode}).';
          }
          return false;
        }

        clearCart();
        _successMessage = 'Pedido modificado con éxito.';
        return true;
      }

      // Create Sales Order request command
      final createBody = {
        'CustomerId': customerId,
        'OrderDate': DateTime.now().toUtc().toIso8601String(),
        'Notes': notes ?? 'Pedido desde POS Móvil (Vendedor)',
        'Details': _cart.map((item) {
          return {
            'ProductId': item.product.id,
            'UnitOfMeasureId': item.presentation.unitOfMeasureId,
            'Quantity': item.quantity,
            'UnitPrice': item.unitPriceDisplayed,
            'DiscountPercentage': 0.0,
            'TaxPercentage': item.presentation.taxPercentage,
          };
        }).toList(),
      };

      // Create sales order
      final createResponse = await apiService.post('/sales-orders', createBody);
      if (createResponse.statusCode != 201 && createResponse.statusCode != 200) {
        try {
          final err = jsonDecode(createResponse.body);
          _errorMessage = err['detail'] ?? err['message'] ?? err['title'] ?? 'Error al crear el pedido (Código ${createResponse.statusCode}).';
        } catch (_) {
          _errorMessage = 'Error al crear el pedido (Código ${createResponse.statusCode}).';
        }
        return false;
      }

      clearCart();
      _successMessage = 'Pedido registrado con éxito.';
      return true;
    } catch (e) {
      _errorMessage = 'Error al registrar pedido: $e';
      return false;
    } finally {
      _isBusy = false;
      notifyListeners();
    }
  }

  // Load an existing order into the cart for editing
  void loadOrderToCart(SalesOrderDetail order, List<Customer> allCustomers, List<Product> catalogProducts) {
    clearCart();
    _editingOrderId = order.id;

    // Find customer in allCustomers list
    Customer? customer;
    for (var c in allCustomers) {
      if (c.id == order.customerId) {
        customer = c;
        break;
      }
    }

    if (customer == null) {
      // Create fallback customer
      customer = Customer(
        id: order.customerId,
        customerCode: order.customerCode,
        name: order.customerName,
        isTaxExempt: false,
        defaultDiscountPercentage: 0.0,
        status: 1,
        canUseCredit: false,
        creditLimit: 0.0,
        creditDays: 0,
        customerPricingProfileType: 0,
        currentDebt: 0.0,
      );
    }
    _selectedCustomer = customer;

    // Load items
    for (var detail in order.details) {
      // Find product in catalog
      Product? product;
      for (var p in catalogProducts) {
        if (p.id == detail.productId) {
          product = p;
          break;
        }
      }

      if (product == null) {
        // Create fallback product
        product = Product(
          id: detail.productId,
          internalCode: detail.productCode,
          name: detail.productName,
          defaultUnitOfMeasureId: detail.unitOfMeasureId,
          defaultUnitOfMeasureCode: detail.unitOfMeasure,
          defaultSalePrice: detail.unitPrice,
          isActive: true,
          isSoldOut: false,
          presentations: [],
          categoryId: '',
        );
      }

      // Find presentation matching unitOfMeasureId
      ProductPresentation? presentation;
      for (var pres in product.presentations) {
        if (pres.unitOfMeasureId == detail.unitOfMeasureId) {
          presentation = pres;
          break;
        }
      }

      if (presentation == null) {
        // Create fallback/dynamic presentation
        presentation = ProductPresentation(
          id: detail.unitOfMeasureId,
          productId: detail.productId,
          name: detail.unitOfMeasure,
          unitOfMeasureId: detail.unitOfMeasureId,
          unitOfMeasureCode: detail.unitOfMeasure,
          conversionFactor: 1.0,
          retailPrice: detail.unitPrice,
          semiWholesalePrice: detail.unitPrice,
          wholesalePrice: detail.unitPrice,
          cost: detail.unitPrice,
          taxPercentage: detail.taxPercentage,
          isBaseUnit: true,
          isDefaultSalePresentation: true,
          isActive: true,
        );
      }

      _cart.add(CartItem(
        product: product,
        presentation: presentation,
        quantity: detail.quantity,
        unitPriceDisplayed: detail.unitPrice,
        lineTotal: detail.quantity * detail.unitPrice,
      ));
    }

    recalculatePricing();
  }
}
