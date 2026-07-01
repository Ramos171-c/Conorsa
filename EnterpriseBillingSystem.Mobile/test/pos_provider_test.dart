import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mobile_app/models/customer.dart';
import 'package:mobile_app/models/product.dart';
import 'package:mobile_app/providers/pos_provider.dart';
import 'package:mobile_app/services/api_service.dart';

// Implicitly implement ApiService to mock it easily without SharedPreferences dependencies
class MockApiService implements ApiService {
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);

  @override
  Future<http.Response> get(String path, {Map<String, String>? headers}) async {
    if (path.contains('top-products')) {
      return http.Response('[]', 200);
    }
    if (path.contains('/cash-sessions')) {
      return http.Response(
        '{"items": [{"id": "session-123", "cashRegisterId": "reg-1", "cashRegisterName": "Caja 1", "openedByUserName": "cajero", "openedAt": "2026-06-20T12:00:00Z", "openingCash": 1000.0, "currentBalance": 1000.0, "status": "Open"}]}',
        200,
      );
    }
    if (path.contains('/Inventory/stock')) {
      return http.Response(
        '{"items": [{"branchWarehouseId": "wh-001"}]}',
        200,
      );
    }
    return http.Response('{"items": []}', 200);
  }

  @override
  Future<http.Response> post(String path, Object body, {Map<String, String>? headers}) async {
    if (path.contains('/sales-invoices') && !path.contains('/post')) {
      return http.Response('"invoice-uuid-12345"', 201);
    }
    if (path.contains('/sales-orders')) {
      return http.Response('"order-uuid-12345"', 201);
    }
    if (path.contains('/post')) {
      return http.Response('', 204);
    }
    return http.Response('{"message": "Error"}', 400);
  }
}

void main() {
  group('POS Provider Pricing Motor Tests', () {
    late PosProvider posProvider;
    late MockApiService mockApi;

    // Test data setup
    final sampleProduct = Product(
      id: 'prod-1',
      internalCode: 'P-001',
      name: 'Gaseosa Litro',
      defaultUnitOfMeasureId: 'uom-1',
      defaultUnitOfMeasureCode: 'LITRO',
      defaultSalePrice: 50.0,
      isActive: true,
      isSoldOut: false,
      presentations: [
        ProductPresentation(
          id: 'pres-1',
          productId: 'prod-1',
          name: 'Botella 1L',
          unitOfMeasureId: 'uom-1',
          unitOfMeasureCode: 'LITRO',
          conversionFactor: 1.0,
          retailPrice: 50.0,
          semiWholesalePrice: 45.0,
          wholesalePrice: 40.0,
          cost: 30.0,
          taxPercentage: 15.0,
          isBaseUnit: true,
          isDefaultSalePresentation: true,
          isActive: true,
        ),
      ],
    );

    final customerDetail = Customer(
      id: 'cust-detail',
      customerCode: 'CUS-001',
      name: 'Juan Perez (Detalle)',
      isTaxExempt: false,
      defaultDiscountPercentage: 0.0,
      status: 1,
      canUseCredit: false,
      creditLimit: 0.0,
      creditDays: 0,
      customerPricingProfileType: 0, // Retail / Detalle
      currentDebt: 0.0,
    );

    final customerWholesale = Customer(
      id: 'cust-wholesale',
      customerCode: 'CUS-002',
      name: 'Distribuidora Norte (Mayorista)',
      isTaxExempt: false,
      defaultDiscountPercentage: 0.0,
      status: 1,
      canUseCredit: true,
      creditLimit: 50000.0,
      creditDays: 30,
      customerPricingProfileType: 2, // Wholesale / Mayorista
      currentDebt: 0.0,
    );

    setUp(() {
      mockApi = MockApiService();
      posProvider = PosProvider(mockApi);
    });

    test('Initial cart is empty and level is DETALLE', () {
      expect(posProvider.cart.isEmpty, true);
      expect(posProvider.currentLevel, 'DETALLE');
      expect(posProvider.subtotalBase, 0.0);
    });

    test('Adding product under 10k threshold keeps level DETALLE', () {
      // 10 units * 50.0 retail = 500.0
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 10.0);

      expect(posProvider.cart.length, 1);
      expect(posProvider.subtotalBase, 500.0);
      expect(posProvider.subtotalCommercial, 500.0);
      expect(posProvider.currentLevel, 'DETALLE');
      expect(posProvider.nextLevel, 'SEMI MAYORISTA');
      expect(posProvider.missingToNextLevel, 9501.0);
    });

    test('Adding product crossing 10k threshold promotes to SEMI MAYORISTA and applies price in caliente', () {
      // 210 units * 50.0 retail = 10,500.0 (crosses 10,000 threshold)
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 210.0);

      expect(posProvider.subtotalBase, 10500.0);
      expect(posProvider.currentLevel, 'SEMI MAYORISTA');
      // Commercial total should use semiWholesalePrice (45.0) -> 210 * 45 = 9,450.0
      expect(posProvider.subtotalCommercial, 9450.0);
      expect(posProvider.nextLevel, 'MAYORISTA');
      expect(posProvider.missingToNextLevel, 19501.0);
    });

    test('Adding product crossing 30k threshold promotes to MAYORISTA and applies wholesale price', () {
      // 610 units * 50.0 retail = 30,500.0 (crosses 30,000 threshold)
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 610.0);

      expect(posProvider.subtotalBase, 30500.0);
      expect(posProvider.currentLevel, 'MAYORISTA');
      // Commercial total uses wholesalePrice (40.0) -> 610 * 40 = 24,400.0
      expect(posProvider.subtotalCommercial, 24400.0);
      expect(posProvider.nextLevel, '');
      expect(posProvider.missingToNextLevel, 0.0);
      expect(posProvider.progressToNextLevel, 1.0);
    });

    test('Customer base profile overrides dynamic tier if higher', () {
      // Set a Mayorista customer
      posProvider.setCustomer(customerWholesale);

      // Add only 1 unit -> subtotal base is 50.0 (which would be DETALLE normally)
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 1.0);

      expect(posProvider.subtotalBase, 50.0);
      // Level is Mayorista because of client profile override
      expect(posProvider.currentLevel, 'MAYORISTA');
      // Commercial price should be wholesalePrice (40.0)
      expect(posProvider.subtotalCommercial, 40.0);
    });

    test('Checkout fails if cart is empty', () async {
      posProvider.setCustomer(customerDetail);
      final success = await posProvider.checkout();
      expect(success, false);
      expect(posProvider.errorMessage, 'El carrito está vacío.');
    });

    test('Checkout fails if no customer is selected', () async {
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 2.0);
      posProvider.setCustomer(null);
      final success = await posProvider.checkout();
      expect(success, false);
      expect(posProvider.errorMessage, 'Debe seleccionar un cliente para procesar el pedido.');
    });

    test('Checkout succeeds with customer and items in cart', () async {
      posProvider.addToCart(sampleProduct, sampleProduct.presentations.first, 2.0);
      posProvider.setCustomer(customerDetail);

      final success = await posProvider.checkout(notes: 'Test order note');
      expect(success, true);
      expect(posProvider.successMessage, 'Pedido registrado con éxito.');
      expect(posProvider.cart.isEmpty, true); // Cart is cleared on success
    });
  });
}
