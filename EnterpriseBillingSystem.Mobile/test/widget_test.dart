// This is a basic Flutter widget test.
//
// To perform an interaction with a widget in your test, use the WidgetTester
// utility in the flutter_test package. For example, you can send tap and scroll
// gestures. You can also use WidgetTester to find child widgets in the widget
// tree, read text, and verify that the values of widget properties are correct.

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:http/http.dart' as http;

import 'package:mobile_app/main.dart';
import 'package:mobile_app/services/api_service.dart';
import 'package:mobile_app/providers/config_provider.dart';
import 'package:mobile_app/providers/auth_provider.dart';
import 'package:mobile_app/providers/order_provider.dart';
import 'package:mobile_app/providers/pos_provider.dart';

class MockApiService implements ApiService {
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);

  @override
  Future<bool> hasSession() async => false;

  @override
  Future<http.Response> get(String path, {Map<String, String>? headers}) async {
    if (path.contains('top-products')) {
      return http.Response('[]', 200);
    }
    return http.Response('{"items": []}', 200);
  }
}

void main() {
  testWidgets('App initialization smoke test', (WidgetTester tester) async {
    final configProvider = ConfigProvider();
    final apiService = MockApiService();
    final authProvider = AuthProvider(apiService);
    final orderProvider = OrderProvider(apiService);
    final posProvider = PosProvider(apiService);

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<ConfigProvider>.value(value: configProvider),
          Provider<ApiService>.value(value: apiService),
          ChangeNotifierProvider<AuthProvider>.value(value: authProvider),
          ChangeNotifierProvider<OrderProvider>.value(value: orderProvider),
          ChangeNotifierProvider<PosProvider>.value(value: posProvider),
        ],
        child: const MyApp(),
      ),
    );

    // Let the checking session and post-frame callbacks complete
    await tester.pumpAndSettle();

    // Now it should land on the CatalogScreen
    expect(find.text('Catálogo de Productos'), findsOneWidget);
  });
}

