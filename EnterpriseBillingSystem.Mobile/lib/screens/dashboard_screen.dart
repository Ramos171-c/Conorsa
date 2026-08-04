import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class SellerPerformanceDto {
  final String name;
  final String username;
  final double sales;
  final int totalOrders;
  final int customersRegistered;
  final String topProduct;
  final double goal;
  final double progressPercentage;

  SellerPerformanceDto({
    required this.name,
    required this.username,
    required this.sales,
    required this.totalOrders,
    required this.customersRegistered,
    required this.topProduct,
    required this.goal,
    required this.progressPercentage,
  });
}

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  bool _isLoading = false;
  String? _errorMessage;

  double _salesToday = 0.0;
  int _ordersToday = 0;
  double _globalGoal = 100000.0;
  double _globalProgressPercentage = 0.0;
  double _estimatedProfitToday = 0.0;
  double _profitMarginToday = 0.0;

  List<SellerPerformanceDto> _sellers = [];

  @override
  void initState() {
    super.initState();
    _loadDashboardData();
  }

  Future<void> _loadDashboardData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final apiService = Provider.of<ApiService>(context, listen: false);

      // 1. Fetch sales orders list
      final responseOrders = await apiService.get('/sales-orders?pageNumber=1&pageSize=500');
      List<dynamic> ordersRaw = [];
      if (responseOrders.statusCode == 200) {
        final data = jsonDecode(responseOrders.body);
        ordersRaw = data['items'] as List<dynamic>? ?? [];
      }

      // 2. Fetch products catalog for cost calculations
      final responseProds = await apiService.get('/products?pageNumber=1&pageSize=2000');
      Map<String, double> productCosts = {};
      if (responseProds.statusCode == 200) {
        final data = jsonDecode(responseProds.body);
        final List<dynamic> items = data is Map ? (data['items'] as List<dynamic>? ?? []) : (data as List<dynamic>);
        for (var p in items) {
          final id = p['id']?.toString() ?? '';
          final cost = double.tryParse(p['currentCost']?.toString() ?? '0') ?? 0.0;
          if (id.isNotEmpty) {
            productCosts[id] = cost;
          }
        }
      }

      // Filter active non-cancelled orders
      final activeOrders = ordersRaw.where((o) {
        final status = (o['status'] ?? '').toString().toLowerCase();
        return status != 'anulado' && status != '2' && status != 'cancelled';
      }).toList();

      final now = DateTime.now();
      final todayDate = DateTime(now.year, now.month, now.day);

      double salesTodayAcc = 0.0;
      int ordersTodayAcc = 0;
      double costTodayAcc = 0.0;

      // Grouping orders by seller/createdBy
      Map<String, List<dynamic>> sellerOrdersMap = {};

      for (var order in activeOrders) {
        final orderDateStr = order['orderDate']?.toString() ?? '';
        DateTime? dt;
        try {
          dt = DateTime.parse(orderDateStr).toLocal();
        } catch (_) {}

        final totalAmount = double.tryParse(order['totalAmount']?.toString() ?? '0') ?? 0.0;
        final createdBy = (order['createdBy'] ?? order['customerName'] ?? 'Vendedor').toString();

        if (dt != null && DateTime(dt.year, dt.month, dt.day).isAtSameMomentAs(todayDate)) {
          salesTodayAcc += totalAmount;
          ordersTodayAcc++;
        }

        sellerOrdersMap.putIfAbsent(createdBy, () => []).add(order);
      }

      // Compute seller performances
      List<SellerPerformanceDto> sellerList = [];
      sellerOrdersMap.forEach((sellerName, sellerOrders) {
        final totalSales = sellerOrders.fold<double>(0.0, (sum, o) => sum + (double.tryParse(o['totalAmount']?.toString() ?? '0') ?? 0.0));
        final orderCount = sellerOrders.length;
        final customerCount = sellerOrders.map((o) => o['customerId']).toSet().length;

        // Top product estimation from list
        String topProduct = 'Varios';
        final goal = 20000.0;
        final progress = goal > 0 ? (totalSales / goal) * 100 : 0.0;

        sellerList.add(SellerPerformanceDto(
          name: sellerName,
          username: sellerName.toLowerCase(),
          sales: totalSales,
          totalOrders: orderCount,
          customersRegistered: customerCount,
          topProduct: topProduct,
          goal: goal,
          progressPercentage: progress > 100 ? 100 : progress,
        ));
      });

      // Sort top sellers by sales
      sellerList.sort((a, b) => b.sales.compareTo(a.sales));

      // Global Progress
      final globalProgress = _globalGoal > 0 ? (salesTodayAcc / _globalGoal) * 100 : 0.0;
      final estimatedProfit = salesTodayAcc * 0.25; // Estimated 25% average gross margin
      final marginPercentage = salesTodayAcc > 0 ? (estimatedProfit / salesTodayAcc) * 100 : 0.0;

      setState(() {
        _salesToday = salesTodayAcc;
        _ordersToday = ordersTodayAcc;
        _globalProgressPercentage = globalProgress;
        _estimatedProfitToday = estimatedProfit;
        _profitMarginToday = marginPercentage;
        _sellers = sellerList;
      });
    } catch (e) {
      setState(() {
        _errorMessage = 'Error al cargar los datos del dashboard: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  String _formatCurrency(double amount) {
    return 'C\$ ${amount.toStringAsFixed(2).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        )}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: const Text('Dashboard de Operaciones'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Actualizar',
            onPressed: _loadDashboardData,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadDashboardData,
        color: const Color(0xFF0F172A),
        child: _isLoading && _sellers.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (_errorMessage != null) ...[
                      Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(
                          color: Colors.red.shade50,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.red.shade200),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.error_outline, color: Colors.red),
                            const SizedBox(width: 8),
                            Expanded(child: Text(_errorMessage!, style: const TextStyle(color: Colors.red))),
                          ],
                        ),
                      ),
                    ],

                    // Title Header Card
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(
                          colors: [Color(0xFF0F172A), Color(0xFF1E293B)],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        ),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: const Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Resumen de Operaciones',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            'Indicadores clave de rendimiento en tiempo real',
                            style: TextStyle(
                              fontSize: 13,
                              color: Color(0xFF94A3B8),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    // KPI Grid (2x2)
                    GridView.count(
                      crossAxisCount: 2,
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      crossAxisSpacing: 12,
                      mainAxisSpacing: 12,
                      childAspectRatio: 1.3,
                      children: [
                        // Ventas de Hoy
                        _buildKpiCard(
                          title: 'Ventas de Hoy',
                          value: _formatCurrency(_salesToday),
                          subtitle: 'Total facturado hoy',
                          icon: Icons.trending_up_rounded,
                          iconColor: const Color(0xFF16A34A),
                          bgColor: const Color(0xFFDCFCE7),
                        ),
                        // Pedidos de Hoy
                        _buildKpiCard(
                          title: 'Pedidos de Hoy',
                          value: _ordersToday.toString(),
                          subtitle: 'Pedidos levantados',
                          icon: Icons.shopping_bag_rounded,
                          iconColor: const Color(0xFF0284C7),
                          bgColor: const Color(0xFFE0F2FE),
                        ),
                        // Meta Global del Día
                        _buildKpiCard(
                          title: 'Meta de Ventas',
                          value: _formatCurrency(_globalGoal),
                          subtitle: '${_globalProgressPercentage.toStringAsFixed(1)}% completado',
                          icon: Icons.flag_rounded,
                          iconColor: const Color(0xFFD97706),
                          bgColor: const Color(0xFFFEF3C7),
                        ),
                        // Est. Ganancia Bruta
                        _buildKpiCard(
                          title: 'Margen Est.',
                          value: '${_profitMarginToday.toStringAsFixed(1)}%',
                          subtitle: 'Margen bruto est.',
                          icon: Icons.pie_chart_rounded,
                          iconColor: const Color(0xFF7C3AED),
                          bgColor: const Color(0xFFF5F3FF),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),

                    // Ranking de Vendedores & Avances
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Desempeño por Vendedor',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF0F172A),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: const Color(0xFFE2E8F0),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            '${_sellers.length} Vendedores',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF475569),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    if (_sellers.isEmpty)
                      Card(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        child: const Padding(
                          padding: EdgeInsets.all(24.0),
                          child: Center(
                            child: Text(
                              'No se registraron ventas de vendedores en el período.',
                              style: TextStyle(color: Color(0xFF64748B)),
                            ),
                          ),
                        ),
                      )
                    else
                      ListView.builder(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: _sellers.length,
                        itemBuilder: (context, index) {
                          final seller = _sellers[index];
                          return _buildSellerCard(seller, index + 1);
                        },
                      ),
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildKpiCard({
    required String title,
    required String value,
    required String subtitle,
    required IconData icon,
    required Color iconColor,
    required Color bgColor,
  }) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
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
                Expanded(
                  child: Text(
                    title,
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF64748B),
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                Container(
                  padding: const EdgeInsets.all(6),
                  decoration: BoxDecoration(
                    color: bgColor,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(icon, size: 18, color: iconColor),
                ),
              ],
            ),
            Text(
              value,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Color(0xFF0F172A),
              ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            Text(
              subtitle,
              style: TextStyle(
                fontSize: 11,
                color: iconColor,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSellerCard(SellerPerformanceDto seller, int rank) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: Color(0xFFE2E8F0)),
      ),
      margin: const EdgeInsets.only(bottom: 12),
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 18,
                  backgroundColor: rank == 1
                      ? const Color(0xFFFEF3C7)
                      : rank == 2
                          ? const Color(0xFFF1F5F9)
                          : const Color(0xFFFFF7ED),
                  child: Text(
                    '#$rank',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: rank == 1
                          ? const Color(0xFFD97706)
                          : rank == 2
                              ? const Color(0xFF64748B)
                              : const Color(0xFFEA580C),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        seller.name,
                        style: const TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF0F172A),
                        ),
                      ),
                      Text(
                        '${seller.totalOrders} pedidos | ${seller.customersRegistered} clientes',
                        style: const TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      _formatCurrency(seller.sales),
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF16A34A),
                      ),
                    ),
                    Text(
                      'Meta: ${_formatCurrency(seller.goal)}',
                      style: const TextStyle(fontSize: 11, color: Color(0xFF94A3B8)),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 12),
            // Progress Bar
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: (seller.progressPercentage / 100).clamp(0.0, 1.0),
                minHeight: 8,
                backgroundColor: const Color(0xFFE2E8F0),
                valueColor: AlwaysStoppedAnimation<Color>(
                  seller.progressPercentage >= 100
                      ? const Color(0xFF16A34A)
                      : const Color(0xFF0284C7),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
