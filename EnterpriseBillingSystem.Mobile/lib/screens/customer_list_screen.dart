import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/order_provider.dart';
import '../models/customer.dart';
import '../models/route.dart';
import '../services/api_service.dart';
import 'register_customer_screen.dart';

class CustomerListScreen extends StatefulWidget {
  const CustomerListScreen({super.key});

  @override
  State<CustomerListScreen> createState() => _CustomerListScreenState();
}

class _CustomerListScreenState extends State<CustomerListScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _refreshList();
    });
  }

  Future<void> _refreshList() async {
    final orderProv = Provider.of<OrderProvider>(context, listen: false);
    // Fetch customers (without route limit for admin) and routes concurrently
    await Future.wait([
      orderProv.fetchCustomers(search: _searchQuery),
      orderProv.fetchRoutes(),
    ]);
  }

  Color _getStatusColor(int status) {
    switch (status) {
      case 1:
        return Colors.green;
      case 2:
        return Colors.red;
      case 3:
        return Colors.grey;
      case 4:
        return Colors.orange;
      default:
        return Colors.grey;
    }
  }

  String _formatCurrency(double amount) {
    return 'C\$ ${amount.toStringAsFixed(2).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        )}';
  }

  void _showDeleteDialog(BuildContext context, Customer customer) {
    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Eliminar Cliente'),
          content: Text('¿Está seguro de que desea eliminar al cliente "${customer.name}"? Esta acción no se puede deshacer.'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancelar'),
            ),
            TextButton(
              onPressed: () async {
                Navigator.pop(context); // close confirm dialog
                
                final orderProv = Provider.of<OrderProvider>(context, listen: false);
                final success = await orderProv.deleteCustomer(customer.id);
                
                if (context.mounted) {
                  if (success) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Cliente eliminado con éxito.')),
                    );
                  } else {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(orderProv.errorMessage ?? 'Error al eliminar cliente.')),
                    );
                  }
                }
              },
              child: const Text('Eliminar', style: TextStyle(color: Colors.red)),
            ),
          ],
        );
      },
    );
  }

  void _editCustomer(BuildContext context, Customer customer) async {
    // Show loading overlay
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final apiService = Provider.of<ApiService>(context, listen: false);
      // Fetch full CustomerDto details
      final response = await apiService.get('/customers/${customer.id}');
      
      if (context.mounted) {
        Navigator.pop(context); // close loader
      }

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        
        if (context.mounted) {
          final result = await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => RegisterCustomerScreen(editCustomerData: data),
            ),
          );
          if (result == true) {
            _refreshList();
          }
        }
      } else {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Error al obtener los detalles del cliente.')),
          );
        }
      }
    } catch (e) {
      if (context.mounted) {
        Navigator.pop(context); // close loader in case of error
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final orderProv = Provider.of<OrderProvider>(context);
    final customers = orderProv.customers;
    
    // Create routes map for easy names resolution
    final Map<String, String> routeNames = {
      for (RouteItem r in orderProv.routes) r.id: r.name
    };

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: const Text('Gestión de Clientes'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
      ),
      body: Column(
        children: [
          // Search box
          Container(
            color: Colors.white,
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Buscar por nombre o código...',
                prefixIcon: const Icon(Icons.search_rounded, color: Color(0xFF64748B)),
                suffixIcon: _searchQuery.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear_rounded, color: Color(0xFF64748B)),
                        onPressed: () {
                          _searchController.clear();
                          setState(() {
                            _searchQuery = '';
                          });
                          orderProv.fetchCustomers(search: '');
                        },
                      )
                    : null,
                contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 16),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
                ),
              ),
              onChanged: (val) {
                setState(() {
                  _searchQuery = val.trim();
                });
                orderProv.fetchCustomers(search: _searchQuery);
              },
            ),
          ),

          // Customer List
          Expanded(
            child: orderProv.isLoading && customers.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : RefreshIndicator(
                    onRefresh: _refreshList,
                    child: customers.isEmpty
                        ? ListView(
                            physics: const AlwaysScrollableScrollPhysics(),
                            children: [
                              SizedBox(height: MediaQuery.of(context).size.height * 0.2),
                              const Center(
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.people_alt_rounded, size: 72, color: Color(0xFFCBD5E1)),
                                    SizedBox(height: 16),
                                    Text(
                                      'No se encontraron clientes',
                                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Color(0xFF64748B)),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          )
                        : ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: customers.length,
                            itemBuilder: (context, index) {
                              final customer = customers[index];
                              final statusColor = _getStatusColor(customer.status);
                              final routeName = routeNames[customer.routeId] ?? 'Sin Ruta';

                              return Card(
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12),
                                  side: const BorderSide(color: Color(0xFFE2E8F0)),
                                ),
                                color: Colors.white,
                                margin: const EdgeInsets.only(bottom: 12),
                                child: Padding(
                                  padding: const EdgeInsets.all(16),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Expanded(
                                            child: Text(
                                              customer.name,
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 16,
                                                color: Color(0xFF1E293B),
                                              ),
                                              maxLines: 1,
                                              overflow: TextOverflow.ellipsis,
                                            ),
                                          ),
                                          const SizedBox(width: 8),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                            decoration: BoxDecoration(
                                              color: statusColor.withOpacity(0.1),
                                              border: Border.all(color: statusColor),
                                              borderRadius: BorderRadius.circular(20),
                                            ),
                                            child: Text(
                                              customer.statusText,
                                              style: TextStyle(
                                                color: statusColor,
                                                fontWeight: FontWeight.bold,
                                                fontSize: 12,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          const Icon(Icons.qr_code_rounded, size: 16, color: Color(0xFF94A3B8)),
                                          const SizedBox(width: 6),
                                          Text(
                                            'Código: ${customer.customerCode}',
                                            style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 4),
                                      Row(
                                        children: [
                                          const Icon(Icons.route_rounded, size: 16, color: Color(0xFF94A3B8)),
                                          const SizedBox(width: 6),
                                          Text(
                                            'Ruta: $routeName',
                                            style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 4),
                                      Row(
                                        children: [
                                          const Icon(Icons.credit_card_rounded, size: 16, color: Color(0xFF94A3B8)),
                                          const SizedBox(width: 6),
                                          Text(
                                            'Límite Crédito: ${customer.canUseCredit ? _formatCurrency(customer.creditLimit) : "No aplica"}',
                                            style: const TextStyle(color: Color(0xFF64748B), fontSize: 13),
                                          ),
                                        ],
                                      ),
                                      const Divider(height: 24),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.end,
                                        children: [
                                          TextButton.icon(
                                            onPressed: () => _editCustomer(context, customer),
                                            icon: const Icon(Icons.edit_outlined, size: 18),
                                            label: const Text('Editar'),
                                          ),
                                          const SizedBox(width: 12),
                                          TextButton.icon(
                                            onPressed: () => _showDeleteDialog(context, customer),
                                            icon: const Icon(Icons.delete_outline_rounded, size: 18, color: Colors.red),
                                            label: const Text('Eliminar', style: TextStyle(color: Colors.red)),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              );
                            },
                          ),
                  ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        onPressed: () async {
          final result = await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => const RegisterCustomerScreen(),
            ),
          );
          if (result == true) {
            _refreshList();
          }
        },
        child: const Icon(Icons.add_rounded),
      ),
    );
  }
}
