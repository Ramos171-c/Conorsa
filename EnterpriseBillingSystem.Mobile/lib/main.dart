import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'providers/config_provider.dart';
import 'providers/auth_provider.dart';
import 'providers/order_provider.dart';
import 'providers/pos_provider.dart';
import 'services/api_service.dart';
import 'screens/catalog_screen.dart';
import 'screens/home_screen.dart';

// Bypass self-signed certificate checks in local development environments
class MyHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback = (X509Certificate cert, String host, int port) => true;
  }
}

void main() async {
  HttpOverrides.global = MyHttpOverrides();
  WidgetsFlutterBinding.ensureInitialized();

  // Create core service instances
  final configProvider = ConfigProvider();
  await configProvider.loadConfig();

  final apiService = ApiService(configProvider);
  final authProvider = AuthProvider(apiService);
  final orderProvider = OrderProvider(apiService);
  final posProvider = PosProvider(apiService);

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: configProvider),
        Provider.value(value: apiService),
        ChangeNotifierProvider.value(value: authProvider),
        ChangeNotifierProvider.value(value: orderProvider),
        ChangeNotifierProvider.value(value: posProvider),
      ],
      child: const MyApp(),
    ),
  );
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'CONORZA',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF0F172A),
          primary: const Color(0xFF0F172A),
          secondary: const Color(0xFF38BDF8),
          surface: const Color(0xFFF8FAFC),
        ),
      ),
      home: const AuthWrapper(),
    );
  }
}

class AuthWrapper extends StatelessWidget {
  const AuthWrapper({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    
    if (auth.isLoading && !auth.isLoggedIn) {
      return const Scaffold(
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              CircularProgressIndicator(),
              SizedBox(height: 16),
              Text(
                'Verificando sesión...',
                style: TextStyle(color: Color(0xFF64748B), fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      );
    }

    if (auth.isLoggedIn) {
      return const HomeScreen();
    }

    return const CatalogScreen();
  }
}
