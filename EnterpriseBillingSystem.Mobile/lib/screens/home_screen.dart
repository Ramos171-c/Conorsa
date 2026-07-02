import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/order_provider.dart';
import '../services/offline_service.dart';
import 'config_screen.dart';
import 'pos_screen.dart';
import 'order_list_screen.dart';
import 'catalog_screen.dart';
import 'register_customer_screen.dart';
import 'goals_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    final profile = auth.userProfile;

    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<OrderProvider>(context, listen: false).syncOfflineData();
    });

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9), // Light grey
      appBar: AppBar(
        title: const Text('Panel de Control'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            tooltip: 'Cerrar Sesión',
            onPressed: () {
              // Confirm logout dialog
              showDialog(
                context: context,
                builder: (context) => AlertDialog(
                  title: const Text('Cerrar Sesión'),
                  content: const Text('¿Está seguro de que desea salir del sistema?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context),
                      child: const Text('Cancelar'),
                    ),
                    TextButton(
                      onPressed: () {
                        Navigator.pop(context);
                        auth.logout();
                      },
                      child: const Text('Salir', style: TextStyle(color: Colors.red)),
                    ),
                  ],
                ),
              );
            },
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // User Header Card
            Container(
              color: const Color(0xFF0F172A),
              padding: const EdgeInsets.only(left: 24, right: 24, bottom: 32, top: 12),
              child: Card(
                color: const Color(0xFF1E293B),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                child: Padding(
                  padding: const EdgeInsets.all(20.0),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 30,
                        backgroundColor: const Color(0xFF38BDF8),
                        child: Text(
                          profile?.firstName.isNotEmpty == true 
                              ? profile!.firstName[0].toUpperCase() 
                              : 'U',
                          style: const TextStyle(
                            fontSize: 24, 
                            fontWeight: FontWeight.bold, 
                            color: Color(0xFF0F172A),
                          ),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              profile?.fullName ?? 'Usuario',
                              style: const TextStyle(
                                fontSize: 18, 
                                fontWeight: FontWeight.bold, 
                                color: Colors.white,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Rol: ${profile?.role ?? 'N/A'}',
                              style: const TextStyle(
                                fontSize: 13, 
                                color: Color(0xFF38BDF8),
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              profile?.email ?? '',
                              style: const TextStyle(fontSize: 13, color: Colors.white60),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            const OfflineSyncBanner(),

            // Dashboard Grid Menu
            Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Operaciones de Venta',
                    style: TextStyle(
                      fontSize: 16, 
                      fontWeight: FontWeight.bold, 
                      color: Color(0xFF475569),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Menu Option: Crear Pedido
                  _buildMenuCard(
                    context,
                    title: 'Crear Pedido',
                    subtitle: 'Levantar pedidos en caliente con precios dinámicos',
                    icon: Icons.add_shopping_cart_rounded,
                    iconBg: const Color(0xFFE0F2FE),
                    iconColor: const Color(0xFF0284C7),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const PosScreen()),
                      );
                    },
                  ),
                  const SizedBox(height: 16),

                  // Menu Option 2: Catalog Grid
                  _buildMenuCard(
                    context,
                    title: 'Ver Catálogo',
                    subtitle: 'Explorar menú de productos en cuadrícula',
                    icon: Icons.restaurant_menu_rounded,
                    iconBg: const Color(0xFFFEF3C7), // Amber Light
                    iconColor: const Color(0xFFD97706), // Amber
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const CatalogScreen()),
                      );
                    },
                  ),
                  const SizedBox(height: 16),

                  // Menu Option 2: Order History
                  _buildMenuCard(
                    context,
                    title: 'Historial de Pedidos',
                    subtitle: 'Ver historial y estado de pedidos levantados',
                    icon: Icons.history_rounded,
                    iconBg: const Color(0xFFF0FDF4),
                    iconColor: const Color(0xFF16A34A),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const OrderListScreen()),
                      );
                    },
                  ),
                  const SizedBox(height: 16),

                  const Text(
                    'Gestión de Campo',
                    style: TextStyle(
                      fontSize: 16, 
                      fontWeight: FontWeight.bold, 
                      color: Color(0xFF475569),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Menu Option: Register Customer
                  _buildMenuCard(
                    context,
                    title: 'Registrar Cliente',
                    subtitle: 'Dar de alta un nuevo cliente en ruta',
                    icon: Icons.person_add_alt_1_rounded,
                    iconBg: const Color(0xFFECFDF5),
                    iconColor: const Color(0xFF059669),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const RegisterCustomerScreen()),
                      );
                    },
                  ),
                  const SizedBox(height: 16),

                  // Menu Option: My Goals
                  _buildMenuCard(
                    context,
                    title: 'Mis Metas',
                    subtitle: 'Ver avance y cuota de ventas del mes',
                    icon: Icons.emoji_events_rounded,
                    iconBg: const Color(0xFFFEF3C7),
                    iconColor: const Color(0xFFD97706),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const GoalsScreen()),
                      );
                    },
                  ),
                  const SizedBox(height: 16),

                  const Text(
                    'Configuración',
                    style: TextStyle(
                      fontSize: 16, 
                      fontWeight: FontWeight.bold, 
                      color: Color(0xFF475569),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Menu Option 3: API settings
                  _buildMenuCard(
                    context,
                    title: 'Conexión de Servidor',
                    subtitle: 'Ajustar dirección base de la API',
                    icon: Icons.settings_input_component_rounded,
                    iconBg: const Color(0xFFF5F3FF),
                    iconColor: const Color(0xFF7C3AED),
                    onTap: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const ConfigScreen()),
                      );
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMenuCard(
    BuildContext context, {
    required String title,
    required String subtitle,
    required IconData icon,
    required Color iconBg,
    required Color iconColor,
    required VoidCallback onTap,
  }) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: Color(0xFFE2E8F0)),
      ),
      color: Colors.white,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: iconBg,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: iconColor, size: 28),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 16, 
                        fontWeight: FontWeight.bold, 
                        color: Color(0xFF1E293B),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      style: const TextStyle(
                        fontSize: 13, 
                        color: Color(0xFF64748B),
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right_rounded, color: Color(0xFF94A3B8)),
            ],
          ),
        ),
      ),
    );
  }
}

class OfflineSyncBanner extends StatelessWidget {
  const OfflineSyncBanner({super.key});

  @override
  Widget build(BuildContext context) {
    final orderProvider = Provider.of<OrderProvider>(context);

    return FutureBuilder<bool>(
      future: OfflineService().hasPendingOfflineData(),
      builder: (context, snapshot) {
        final hasOfflineData = snapshot.data ?? false;
        if (!hasOfflineData && !orderProvider.isSyncing) {
          return const SizedBox.shrink();
        }

        return Card(
          margin: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
          color: orderProvider.isSyncing ? const Color(0xFFEFF6FF) : const Color(0xFFFFF7ED),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
            side: BorderSide(
              color: orderProvider.isSyncing ? const Color(0xFFBFDBFE) : const Color(0xFFFED7AA),
              width: 1,
            ),
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 12.0),
            child: Row(
              children: [
                Icon(
                  orderProvider.isSyncing ? Icons.sync_rounded : Icons.cloud_off_rounded,
                  color: orderProvider.isSyncing ? const Color(0xFF2563EB) : const Color(0xFFEA580C),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        orderProvider.isSyncing ? 'Sincronizando...' : 'Datos Pendientes Offline',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: orderProvider.isSyncing ? const Color(0xFF1E3A8A) : const Color(0xFF7C2D12),
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        orderProvider.isSyncing
                            ? 'Subiendo pedidos y clientes registrados sin internet...'
                            : 'Tienes datos guardados localmente. Presiona el botón para sincronizarlos con el servidor.',
                        style: TextStyle(
                          fontSize: 12,
                          color: orderProvider.isSyncing ? const Color(0xFF3B82F6) : const Color(0xFF9A3412),
                        ),
                      ),
                    ],
                  ),
                ),
                if (!orderProvider.isSyncing)
                  IconButton(
                    icon: const Icon(Icons.sync_rounded, color: Color(0xFFEA580C)),
                    tooltip: 'Sincronizar ahora',
                    onPressed: () async {
                      await orderProvider.syncOfflineData();
                      // Force rebuild
                      (context as Element).markNeedsBuild();
                    },
                  )
                else
                  const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor: AlwaysStoppedAnimation<Color>(Color(0xFF2563EB)),
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}
