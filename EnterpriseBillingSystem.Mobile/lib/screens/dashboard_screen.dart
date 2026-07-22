import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/order_provider.dart';
import '../models/order.dart';
import '../models/route.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late DateTime _fromDate;
  late DateTime _toDate;
  String? _selectedRouteId;
  bool _isInit = true;

  @override
  void initState() {
    super.initState();
    // Default to the current day (start of today to end of today)
    final now = DateTime.now();
    _fromDate = DateTime(now.year, now.month, now.day, 0, 0, 0);
    _toDate = DateTime(now.year, now.month, now.day, 23, 59, 59, 999);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_isInit) {
      final authProv = Provider.of<AuthProvider>(context, listen: false);
      final orderProv = Provider.of<OrderProvider>(context, listen: false);

      // Fetch routes if user is admin
      if (authProv.userProfile?.isAdmin == true) {
        orderProv.fetchRoutes();
      } else {
        // If not admin, restrict to their assigned routeId
        _selectedRouteId = authProv.userProfile?.effectiveRouteId;
      }

      _fetchDashboardData();
      _isInit = false;
    }
  }

  Future<void> _fetchDashboardData() async {
    final orderProv = Provider.of<OrderProvider>(context, listen: false);
    await orderProv.fetchDashboardOrders(
      fromDate: _fromDate,
      toDate: _toDate,
      routeId: _selectedRouteId,
    );
  }

  String _formatCurrency(double amount) {
    return 'C\$ ${amount.toStringAsFixed(2).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        )}';
  }

  String _formatDateShort(DateTime date) {
    return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
  }

  Future<void> _selectDate(BuildContext context, bool isFromDate) async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: isFromDate ? _fromDate : _toDate,
      firstDate: DateTime(2025),
      lastDate: DateTime(2030),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: Color(0xFF0F172A),
              onPrimary: Colors.white,
              onSurface: Color(0xFF1E293B),
            ),
          ),
          child: child!,
        );
      },
    );
    if (picked != null) {
      setState(() {
        if (isFromDate) {
          _fromDate = DateTime(picked.year, picked.month, picked.day, 0, 0, 0);
        } else {
          _toDate = DateTime(picked.year, picked.month, picked.day, 23, 59, 59, 999);
        }
      });
      _fetchDashboardData();
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
      case 'borrador':
      case '0':
        return Colors.orange;
      case 'confirmed':
      case 'confirmado':
      case '1':
        return Colors.blue;
      case 'cancelled':
      case 'anulado':
      case '2':
        return Colors.red;
      case 'completed':
      case 'completado':
        return Colors.green;
      case 'solicitudanulacion':
      case 'solicitud_anulacion':
      case '7':
        return Colors.purple;
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(String status) {
    switch (status.toLowerCase()) {
      case 'draft':
      case '0':
        return 'Borrador';
      case 'confirmed':
      case '1':
        return 'Confirmado';
      case 'cancelled':
      case '2':
        return 'Anulado';
      case 'completed':
        return 'Completado';
      case 'solicitudanulacion':
      case 'solicitud_anulacion':
      case '7':
        return 'Sol. Anulación';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final orderProv = Provider.of<OrderProvider>(context);
    final authProv = Provider.of<AuthProvider>(context);
    final isAdmin = authProv.userProfile?.isAdmin ?? false;

    // Calculations
    final List<SalesOrderListItem> orders = orderProv.dashboardOrders;
    final totalSales = orders
        .where((o) =>
            o.status.toLowerCase() != 'cancelled' &&
            o.status.toLowerCase() != 'anulado' &&
            o.status != '2')
        .fold<double>(0.0, (sum, order) => sum + order.totalAmount);
    final totalCount = orders.length;
    final averageTicket = totalCount > 0 ? totalSales / totalCount : 0.0;

    // Status breakdown counts
    final statusCounts = <String, int>{};
    final statusAmounts = <String, double>{};
    for (var order in orders) {
      final status = _getStatusText(order.status);
      statusCounts[status] = (statusCounts[status] ?? 0) + 1;
      statusAmounts[status] = (statusAmounts[status] ?? 0.0) + order.totalAmount;
    }

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: const Text('Dashboard de Preventas'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Actualizar datos',
            onPressed: _fetchDashboardData,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Panel
          Container(
            color: Colors.white,
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                // Date Range Pickers
                Row(
                  children: [
                    Expanded(
                      child: InkWell(
                        onTap: () => _selectDate(context, true),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                          decoration: BoxDecoration(
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.calendar_today_rounded, size: 16, color: Color(0xFF64748B)),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text('DESDE', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF94A3B8))),
                                    Text(_formatDateShort(_fromDate), style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF1E293B))),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: InkWell(
                        onTap: () => _selectDate(context, false),
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                          decoration: BoxDecoration(
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.calendar_today_rounded, size: 16, color: Color(0xFF64748B)),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text('HASTA', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF94A3B8))),
                                    Text(_formatDateShort(_toDate), style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: Color(0xFF1E293B))),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                
                // Route filter for admins
                if (isAdmin) ...[
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    decoration: BoxDecoration(
                      border: Border.all(color: const Color(0xFFE2E8F0)),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButtonFormField<String?>(
                        value: _selectedRouteId,
                        decoration: const InputDecoration(
                          labelText: 'Filtrar por Ruta',
                          border: InputBorder.none,
                          prefixIcon: Icon(Icons.route_rounded, color: Color(0xFF475569)),
                        ),
                        items: [
                          const DropdownMenuItem<String?>(
                            value: null,
                            child: Text('Todas las rutas'),
                          ),
                          ...orderProv.routes.map((RouteItem route) {
                            return DropdownMenuItem<String?>(
                              value: route.id,
                              child: Text('${route.code} - ${route.name}'),
                            );
                          }),
                        ],
                        onChanged: (val) {
                          setState(() {
                            _selectedRouteId = val;
                          });
                          _fetchDashboardData();
                        },
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),

          // Main content
          Expanded(
            child: orderProv.isLoading && orders.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : RefreshIndicator(
                    onRefresh: _fetchDashboardData,
                    child: SingleChildScrollView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          // Total Card with gradient background
                          Container(
                            decoration: BoxDecoration(
                              gradient: const LinearGradient(
                                colors: [Color(0xFF0F172A), Color(0xFF1E293B)],
                                begin: Alignment.topLeft,
                                end: Alignment.bottomRight,
                              ),
                              borderRadius: BorderRadius.circular(16),
                              boxShadow: [
                                BoxShadow(
                                  color: const Color(0xFF0F172A).withOpacity(0.15),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ],
                            ),
                            padding: const EdgeInsets.all(20),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Text(
                                      'Monto Total Preventa',
                                      style: TextStyle(
                                        color: Colors.white70,
                                        fontSize: 14,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                    Icon(
                                      Icons.monetization_on_rounded,
                                      color: Color(0xFF38BDF8),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  _formatCurrency(totalSales),
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 28,
                                    fontWeight: FontWeight.bold,
                                    letterSpacing: -0.5,
                                  ),
                                ),
                                const SizedBox(height: 12),
                                Container(
                                  height: 1,
                                  color: Colors.white12,
                                ),
                                const SizedBox(height: 12),
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Row(
                                      children: [
                                        const Icon(Icons.shopping_bag_rounded, color: Colors.white54, size: 16),
                                        const SizedBox(width: 6),
                                        Text(
                                          'Pedidos: $totalCount',
                                          style: const TextStyle(color: Colors.white70, fontSize: 13),
                                        ),
                                      ],
                                    ),
                                    Row(
                                      children: [
                                        const Icon(Icons.analytics_rounded, color: Colors.white54, size: 16),
                                        const SizedBox(width: 6),
                                        Text(
                                          'Promedio: ${_formatCurrency(averageTicket)}',
                                          style: const TextStyle(color: Colors.white70, fontSize: 13),
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 20),

                          // Breakdown section title
                          const Text(
                            'Resumen por Estado',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF334155),
                            ),
                          ),
                          const SizedBox(height: 10),

                          // Breakdown Cards Grid
                          if (statusCounts.isEmpty)
                            Card(
                              elevation: 0,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                                side: const BorderSide(color: Color(0xFFE2E8F0)),
                              ),
                              child: const Padding(
                                padding: EdgeInsets.all(16.0),
                                child: Text(
                                  'Sin datos de estados en este periodo.',
                                  textAlign: TextAlign.center,
                                  style: TextStyle(color: Color(0xFF64748B)),
                                ),
                              ),
                            )
                          else
                            GridView.builder(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: 2,
                                crossAxisSpacing: 12,
                                mainAxisSpacing: 12,
                                childAspectRatio: 1.5,
                              ),
                              itemCount: statusCounts.length,
                              itemBuilder: (context, index) {
                                final statusName = statusCounts.keys.elementAt(index);
                                final count = statusCounts[statusName] ?? 0;
                                final amount = statusAmounts[statusName] ?? 0.0;
                                final color = _getStatusColor(statusName);

                                return Card(
                                  elevation: 0,
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                    side: const BorderSide(color: Color(0xFFE2E8F0)),
                                  ),
                                  color: Colors.white,
                                  child: Padding(
                                    padding: const EdgeInsets.all(12.0),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Row(
                                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                          children: [
                                            Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                              decoration: BoxDecoration(
                                                color: color.withOpacity(0.1),
                                                borderRadius: BorderRadius.circular(8),
                                              ),
                                              child: Text(
                                                statusName,
                                                style: TextStyle(
                                                  color: color,
                                                  fontWeight: FontWeight.bold,
                                                  fontSize: 11,
                                                ),
                                              ),
                                            ),
                                            Text(
                                              '#$count',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                color: Color(0xFF64748B),
                                                fontSize: 12,
                                              ),
                                            ),
                                          ],
                                        ),
                                        Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            const Text(
                                              'Total:',
                                              style: TextStyle(fontSize: 10, color: Color(0xFF94A3B8)),
                                            ),
                                            Text(
                                              _formatCurrency(amount),
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 14,
                                                color: Color(0xFF1E293B),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),
                                );
                              },
                            ),
                          const SizedBox(height: 24),

                          // Orders list section title
                          Text(
                            'Pedidos del Periodo ($totalCount)',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF334155),
                            ),
                          ),
                          const SizedBox(height: 10),

                          if (orders.isEmpty)
                            Card(
                              elevation: 0,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                                side: const BorderSide(color: Color(0xFFE2E8F0)),
                              ),
                              color: Colors.white,
                              child: const Padding(
                                padding: EdgeInsets.all(24.0),
                                child: Column(
                                  children: [
                                    Icon(Icons.inbox_rounded, size: 40, color: Color(0xFFCBD5E1)),
                                    SizedBox(height: 8),
                                    Text(
                                      'No se encontraron pedidos para este rango.',
                                      textAlign: TextAlign.center,
                                      style: TextStyle(color: Color(0xFF64748B), fontSize: 13),
                                    ),
                                  ],
                                ),
                              ),
                            )
                          else
                            ListView.builder(
                              shrinkWrap: true,
                              physics: const NeverScrollableScrollPhysics(),
                              itemCount: orders.length,
                              itemBuilder: (context, index) {
                                final order = orders[index];
                                final statusColor = _getStatusColor(order.status);
                                final orderDateStr = _formatDateShort(order.orderDate);

                                return Card(
                                  elevation: 0,
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(10),
                                    side: const BorderSide(color: Color(0xFFE2E8F0)),
                                  ),
                                  color: Colors.white,
                                  margin: const EdgeInsets.only(bottom: 8),
                                  child: ListTile(
                                    contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                                    title: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          '#${order.orderNumber}',
                                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF1E293B)),
                                        ),
                                        Text(
                                          _formatCurrency(order.totalAmount),
                                          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF0F172A)),
                                        ),
                                      ],
                                    ),
                                    subtitle: Padding(
                                      padding: const EdgeInsets.only(top: 4.0),
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            order.customerName,
                                            style: const TextStyle(fontSize: 12, color: Color(0xFF64748B), fontWeight: FontWeight.w500),
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                          ),
                                          const SizedBox(height: 2),
                                          Row(
                                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                            children: [
                                              Text(
                                                orderDateStr,
                                                style: const TextStyle(fontSize: 11, color: Color(0xFF94A3B8)),
                                              ),
                                              Container(
                                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                                decoration: BoxDecoration(
                                                  color: statusColor.withOpacity(0.1),
                                                  borderRadius: BorderRadius.circular(4),
                                                ),
                                                child: Text(
                                                  _getStatusText(order.status),
                                                  style: TextStyle(
                                                    color: statusColor,
                                                    fontSize: 10,
                                                    fontWeight: FontWeight.bold,
                                                  ),
                                                ),
                                              ),
                                            ],
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                );
                              },
                            ),
                        ],
                      ),
                    ),
                  ),
          ),
        ],
      ),
    );
  }
}
