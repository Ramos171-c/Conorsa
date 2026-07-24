import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/order_provider.dart';
import '../providers/pos_provider.dart';
import '../providers/auth_provider.dart';
import '../models/order.dart';
import '../models/route.dart';
import 'pos_screen.dart';

class OrderListScreen extends StatefulWidget {
  const OrderListScreen({super.key});

  @override
  State<OrderListScreen> createState() => _OrderListScreenState();
}

class _OrderListScreenState extends State<OrderListScreen> {
  String? _selectedRouteId;
  DateTime? _fromDate;
  DateTime? _toDate;
  String _dateFilterLabel = 'Todas las fechas';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final authProv = Provider.of<AuthProvider>(context, listen: false);
      if (authProv.userProfile?.isAdmin == true) {
        Provider.of<OrderProvider>(context, listen: false).fetchRoutes();
      }
      Provider.of<OrderProvider>(context, listen: false).fetchOrders(
        routeId: _selectedRouteId,
        fromDate: _fromDate,
        toDate: _toDate,
      );
    });
  }

  void _selectDateRange() async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2024, 1, 1),
      lastDate: DateTime(now.year + 1, 12, 31),
      initialDateRange: _fromDate != null && _toDate != null
          ? DateTimeRange(start: _fromDate!, end: _toDate!)
          : DateTimeRange(start: now.subtract(const Duration(days: 7)), end: now),
    );

    if (picked != null) {
      setState(() {
        _fromDate = DateTime(picked.start.year, picked.start.month, picked.start.day, 0, 0, 0);
        _toDate = DateTime(picked.end.year, picked.end.month, picked.end.day, 23, 59, 59);
        _dateFilterLabel = '${picked.start.day}/${picked.start.month} - ${picked.end.day}/${picked.end.month}';
      });
      Provider.of<OrderProvider>(context, listen: false).fetchOrders(
        routeId: _selectedRouteId,
        fromDate: _fromDate,
        toDate: _toDate,
      );
    }
  }

  void _setDateFilterQuick(String filterType) {
    final now = DateTime.now();
    DateTime? start;
    DateTime? end;
    String label = 'Todas las fechas';

    if (filterType == 'today') {
      start = DateTime(now.year, now.month, now.day, 0, 0, 0);
      end = DateTime(now.year, now.month, now.day, 23, 59, 59);
      label = 'Hoy';
    } else if (filterType == 'yesterday') {
      final y = now.subtract(const Duration(days: 1));
      start = DateTime(y.year, y.month, y.day, 0, 0, 0);
      end = DateTime(y.year, y.month, y.day, 23, 59, 59);
      label = 'Ayer';
    } else if (filterType == 'month') {
      start = DateTime(now.year, now.month, 1, 0, 0, 0);
      end = DateTime(now.year, now.month + 1, 0, 23, 59, 59);
      label = 'Este Mes';
    }

    setState(() {
      _fromDate = start;
      _toDate = end;
      _dateFilterLabel = label;
    });

    Provider.of<OrderProvider>(context, listen: false).fetchOrders(
      routeId: _selectedRouteId,
      fromDate: _fromDate,
      toDate: _toDate,
    );
  }

  // Voucher Delivery Ticket Dialog
  void _showDeliveryVoucher(BuildContext context, SalesOrderDetail detail) {
    final formattedDate = '${detail.orderDate.day.toString().padLeft(2, '0')}/${detail.orderDate.month.toString().padLeft(2, '0')}/${detail.orderDate.year} ${detail.orderDate.hour.toString().padLeft(2, '0')}:${detail.orderDate.minute.toString().padLeft(2, '0')}';
    final totalUsd = detail.totalAmount / 36.5;

    final StringBuffer ticketBuffer = StringBuffer();
    ticketBuffer.writeln('=================================');
    ticketBuffer.writeln('       CONORZA - DISTRIBUIDORA   ');
    ticketBuffer.writeln('       VOUCHER DE ENTREGA        ');
    ticketBuffer.writeln('=================================');
    ticketBuffer.writeln('Pedido No: ${detail.orderNumber}');
    ticketBuffer.writeln('Fecha:     $formattedDate');
    ticketBuffer.writeln('Cliente:   ${detail.customerName}');
    ticketBuffer.writeln('Estado:    ${_getStatusText(detail.status)}');
    ticketBuffer.writeln('---------------------------------');
    ticketBuffer.writeln('PRODUCTOS:');
    for (var item in detail.details) {
      ticketBuffer.writeln('${item.productName}');
      ticketBuffer.writeln('  ${item.quantity.toStringAsFixed(0)} ${item.unitOfMeasure} x C\$${item.unitPrice.toStringAsFixed(2)} = C\$${item.netAmount.toStringAsFixed(2)}');
    }
    ticketBuffer.writeln('---------------------------------');
    ticketBuffer.writeln('SUBTOTAL:  C\$${detail.subTotal.toStringAsFixed(2)}');
    if (detail.discountAmount > 0) {
      ticketBuffer.writeln('DESCUENTO: -C\$${detail.discountAmount.toStringAsFixed(2)}');
    }
    ticketBuffer.writeln('IVA:       C\$${detail.taxAmount.toStringAsFixed(2)}');
    ticketBuffer.writeln('TOTAL C\$: C\$${detail.totalAmount.toStringAsFixed(2)}');
    ticketBuffer.writeln('TOTAL USD: \$${totalUsd.toStringAsFixed(2)}');
    ticketBuffer.writeln('=================================');
    if (detail.notes != null && detail.notes!.isNotEmpty) {
      ticketBuffer.writeln('NOTAS: ${detail.notes}');
      ticketBuffer.writeln('=================================');
    }

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: Row(
            children: [
              const Icon(Icons.receipt_long_rounded, color: Color(0xFF1E3A8A)),
              const SizedBox(width: 8),
              const Text('Voucher de Entrega', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
            ],
          ),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8FAFC),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFFCBD5E1)),
                  ),
                  child: Text(
                    ticketBuffer.toString(),
                    style: const TextStyle(fontFamily: 'monospace', fontSize: 12, height: 1.3, color: Color(0xFF1E293B)),
                  ),
                ),
              ],
            ),
          ),
          actions: [
            TextButton.icon(
              onPressed: () => Navigator.pop(context),
              icon: const Icon(Icons.close),
              label: const Text('Cerrar'),
            ),
            ElevatedButton.icon(
              onPressed: () {
                Navigator.pop(context);
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Voucher de entrega preparado para impresión.')),
                );
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF1E3A8A),
                foregroundColor: Colors.white,
              ),
              icon: const Icon(Icons.print_rounded),
              label: const Text('Imprimir Voucher'),
            ),
          ],
        );
      },
    );
  }

  // Get status color
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

  // Format Status text
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

  // Cancel order dialog
  void _cancelOrderDialog(BuildContext context, String orderId) {
    final reasonController = TextEditingController();
    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Solicitar Anulación'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('¿Está seguro de que desea solicitar la anulación de este pedido?'),
              const SizedBox(height: 12),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(
                  labelText: 'Motivo de la solicitud',
                  border: OutlineInputBorder(),
                ),
                maxLines: 2,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancelar'),
            ),
            TextButton(
              onPressed: () async {
                final reason = reasonController.text.trim();
                if (reason.isEmpty) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Debe ingresar un motivo de solicitud.')),
                  );
                  return;
                }
                Navigator.pop(context);
                final orderProv = Provider.of<OrderProvider>(context, listen: false);
                final success = await orderProv.cancelOrder(orderId, reason);
                if (success) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Solicitud de anulación enviada con éxito.')),
                  );
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(orderProv.errorMessage ?? 'Error al solicitar la anulación.')),
                  );
                }
              },
              child: const Text('Solicitar', style: TextStyle(color: Colors.red)),
            ),
          ],
        );
      },
    );
  }

  // Edit order handler
  void _editOrder(BuildContext context, SalesOrderListItem orderItem) async {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final orderProv = Provider.of<OrderProvider>(context, listen: false);
      final posProv = Provider.of<PosProvider>(context, listen: false);
      final authProv = Provider.of<AuthProvider>(context, listen: false);
      
      final detailFuture = orderProv.fetchOrderDetail(orderItem.id);
      final productsFuture = orderProv.products.isEmpty ? orderProv.fetchProducts() : Future.value();
      final customersFuture = orderProv.customers.isEmpty ? orderProv.fetchCustomers(routeId: authProv.userProfile?.effectiveRouteId) : Future.value();

      await Future.wait([detailFuture, productsFuture, customersFuture]);
      final detail = await detailFuture;

      if (!context.mounted) return;
      Navigator.pop(context);

      if (detail != null) {
        posProv.loadOrderToCart(detail, orderProv.customers, orderProv.products);
        
        Navigator.push(
          context,
          MaterialPageRoute(builder: (context) => const PosScreen()),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(orderProv.errorMessage ?? 'Error al cargar detalles para edición.')),
        );
      }
    } catch (e) {
      if (context.mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al editar: $e')),
        );
      }
    }
  }

  // Detailed view of an order
  void _showOrderDetails(BuildContext context, String orderId) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) {
        final orderProv = Provider.of<OrderProvider>(context, listen: false);
        return Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.only(
              topLeft: Radius.circular(20),
              topRight: Radius.circular(20),
            ),
          ),
          padding: const EdgeInsets.all(20),
          height: MediaQuery.of(context).size.height * 0.80,
          child: FutureBuilder<SalesOrderDetail?>(
            future: orderProv.fetchOrderDetail(orderId),
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(child: CircularProgressIndicator());
              }
              if (snapshot.hasError || snapshot.data == null) {
                return Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: Colors.red),
                      const SizedBox(height: 8),
                      Text(orderProv.errorMessage ?? 'Error al cargar los detalles.'),
                    ],
                  ),
                );
              }

              final detail = snapshot.data!;
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Pedido #${detail.orderNumber}',
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: Color(0xFF0F172A)),
                      ),
                      IconButton(
                        onPressed: () => Navigator.pop(context),
                        icon: const Icon(Icons.close),
                      )
                    ],
                  ),
                  const Divider(),
                  Expanded(
                    child: ListView(
                      children: [
                        _buildDetailRow('Cliente', detail.customerName),
                        _buildDetailRow('Fecha', '${detail.orderDate.day}/${detail.orderDate.month}/${detail.orderDate.year}'),
                        _buildDetailRow('Estado', _getStatusText(detail.status)),
                        if (detail.createdBy != null && detail.createdBy!.isNotEmpty)
                          _buildDetailRow('Vendedor', detail.createdBy!),
                        if (detail.notes != null && detail.notes!.isNotEmpty)
                          _buildDetailRow('Notas', detail.notes!),
                        const SizedBox(height: 16),
                        const Text(
                          'Productos',
                          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF475569)),
                        ),
                        const SizedBox(height: 8),
                        ...detail.details.map((item) {
                          return Padding(
                            padding: const EdgeInsets.symmetric(vertical: 6.0),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        item.productName,
                                        style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                                      ),
                                      Text(
                                        '${item.quantity.toStringAsFixed(0)} ${item.unitOfMeasure} x \$${item.unitPrice.toStringAsFixed(2)}',
                                        style: TextStyle(color: Colors.grey.shade600, fontSize: 12),
                                      ),
                                    ],
                                  ),
                                ),
                                Text(
                                  '\$${item.netAmount.toStringAsFixed(2)}',
                                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                                ),
                              ],
                            ),
                          );
                        }).toList(),
                      ],
                    ),
                  ),
                  const Divider(),
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4.0),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Subtotal', style: TextStyle(color: Color(0xFF64748B))),
                        Text('\$${detail.subTotal.toStringAsFixed(2)}'),
                      ],
                    ),
                  ),
                  if (detail.discountAmount > 0)
                    Padding(
                      padding: const EdgeInsets.symmetric(vertical: 4.0),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Descuento', style: TextStyle(color: Color(0xFF64748B))),
                          Text('-\$${detail.discountAmount.toStringAsFixed(2)}', style: const TextStyle(color: Colors.red)),
                        ],
                      ),
                    ),
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4.0),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('IVA / Impuestos', style: TextStyle(color: Color(0xFF64748B))),
                        Text('\$${detail.taxAmount.toStringAsFixed(2)}'),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Total',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Color(0xFF0F172A)),
                      ),
                      Text(
                        '\$${detail.totalAmount.toStringAsFixed(2)}',
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 20, color: Color(0xFF1E3A8A)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: () {
                        Navigator.pop(context);
                        _showDeliveryVoucher(context, detail);
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF0F172A),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      icon: const Icon(Icons.receipt_long_rounded),
                      label: const Text('Ver / Imprimir Voucher de Entrega', style: TextStyle(fontWeight: FontWeight.bold)),
                    ),
                  ),
                ],
              );
            },
          ),
        );
      },
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 80,
            child: Text(
              label,
              style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF64748B), fontSize: 13),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(color: Color(0xFF1E293B), fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final provider = Provider.of<OrderProvider>(context);

    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9),
      appBar: AppBar(
        title: const Text('Historial de Pedidos'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.calendar_month_rounded),
            tooltip: 'Filtrar por Rango de Fechas',
            onPressed: _selectDateRange,
          ),
        ],
      ),
      body: Column(
        children: [
          // Filter Section (Date & Route)
          Container(
            color: Colors.white,
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: Column(
              children: [
                // Date Filter Chips Bar
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      const Icon(Icons.filter_list_rounded, size: 18, color: Color(0xFF64748B)),
                      const SizedBox(width: 8),
                      ActionChip(
                        avatar: const Icon(Icons.calendar_today, size: 14),
                        label: Text(_dateFilterLabel),
                        onPressed: _selectDateRange,
                        backgroundColor: _fromDate != null ? const Color(0xFFDBEAFE) : const Color(0xFFF1F5F9),
                        labelStyle: TextStyle(
                          color: _fromDate != null ? const Color(0xFF1E40AF) : const Color(0xFF475569),
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(width: 6),
                      ChoiceChip(
                        label: const Text('Hoy'),
                        selected: _dateFilterLabel == 'Hoy',
                        onSelected: (sel) => _setDateFilterQuick('today'),
                      ),
                      const SizedBox(width: 6),
                      ChoiceChip(
                        label: const Text('Ayer'),
                        selected: _dateFilterLabel == 'Ayer',
                        onSelected: (sel) => _setDateFilterQuick('yesterday'),
                      ),
                      const SizedBox(width: 6),
                      ChoiceChip(
                        label: const Text('Este Mes'),
                        selected: _dateFilterLabel == 'Este Mes',
                        onSelected: (sel) => _setDateFilterQuick('month'),
                      ),
                      if (_fromDate != null) ...[
                        const SizedBox(width: 6),
                        IconButton(
                          icon: const Icon(Icons.clear, size: 18, color: Colors.red),
                          tooltip: 'Limpiar filtro de fecha',
                          onPressed: () => _setDateFilterQuick('all'),
                        ),
                      ],
                    ],
                  ),
                ),
                if (Provider.of<AuthProvider>(context, listen: false).userProfile?.isAdmin == true) ...[
                  const SizedBox(height: 8),
                  Card(
                    elevation: 0,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                      side: const BorderSide(color: Color(0xFFE2E8F0)),
                    ),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12),
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
                            ...provider.routes.map((RouteItem route) {
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
                            provider.fetchOrders(routeId: val, fromDate: _fromDate, toDate: _toDate);
                          },
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => provider.fetchOrders(routeId: _selectedRouteId, fromDate: _fromDate, toDate: _toDate),
              child: provider.isLoading && provider.orders.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : provider.orders.isEmpty
                      ? ListView(
                          physics: const AlwaysScrollableScrollPhysics(),
                          children: [
                            SizedBox(height: MediaQuery.of(context).size.height * 0.2),
                            const Center(
                              child: Column(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Icon(Icons.history_toggle_off_rounded, size: 70, color: Color(0xFFCBD5E1)),
                                  SizedBox(height: 16),
                                  Text(
                                    'No hay pedidos registrados',
                                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Color(0xFF757575)),
                                  ),
                                  SizedBox(height: 6),
                                  Text(
                                    'Los pedidos que registre se mostrarán aquí.',
                                    style: TextStyle(color: Color(0xFF64748B)),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        )
                      : ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: provider.orders.length,
                          itemBuilder: (context, index) {
                            final order = provider.orders[index];
                            final statusColor = _getStatusColor(order.status);
                            final formattedDate = '${order.orderDate.day}/${order.orderDate.month}/${order.orderDate.year}';

                            final isAdmin = Provider.of<AuthProvider>(context, listen: false).userProfile?.isAdmin == true;
                            final difference = DateTime.now().difference(order.orderDate.toLocal());
                            final statusLower = order.status.toLowerCase();
                            final canCancel = statusLower == 'recibido' ||
                                statusLower == '2' ||
                                statusLower == 'enproceso' ||
                                statusLower == '4';
                            // Admins can edit anytime; Sellers have a 10-minute window
                            final canEdit = canCancel && (isAdmin || difference.inMinutes < 10);

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
                                  crossAxisAlignment: CrossAxisAlignment.stretch,
                                  children: [
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Pedido #${order.orderNumber}',
                                          style: const TextStyle(
                                            fontWeight: FontWeight.bold,
                                            fontSize: 16,
                                            color: Color(0xFF1E293B),
                                          ),
                                        ),
                                        Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                          decoration: BoxDecoration(
                                            color: statusColor.withOpacity(0.1),
                                            border: Border.all(color: statusColor),
                                            borderRadius: BorderRadius.circular(20),
                                          ),
                                          child: Text(
                                            _getStatusText(order.status),
                                            style: TextStyle(
                                              color: statusColor,
                                              fontWeight: FontWeight.bold,
                                              fontSize: 12,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 12),
                                    Row(
                                      children: [
                                        Icon(Icons.person, size: 16, color: Colors.grey.shade400),
                                        const SizedBox(width: 6),
                                        Expanded(
                                          child: Text(
                                            order.customerName,
                                            style: const TextStyle(
                                              fontSize: 14,
                                              fontWeight: FontWeight.w600,
                                              color: Color(0xFF475569),
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 6),
                                    Row(
                                      children: [
                                        Icon(Icons.calendar_today, size: 14, color: Colors.grey.shade400),
                                        const SizedBox(width: 8),
                                        Text(
                                          formattedDate,
                                          style: TextStyle(fontSize: 13, color: Colors.grey.shade500),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 12),
                                    const Divider(),
                                    const SizedBox(height: 8),
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        const Text(
                                          'Total del Pedido:',
                                          style: TextStyle(
                                            fontSize: 14,
                                            color: Color(0xFF64748B),
                                            fontWeight: FontWeight.w500,
                                          ),
                                        ),
                                        Text(
                                          '\$${order.totalAmount.toStringAsFixed(2)}',
                                          style: const TextStyle(
                                            fontWeight: FontWeight.bold,
                                            fontSize: 18,
                                            color: Color(0xFF1E3A8A),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 8),
                                    const Divider(),
                                    SingleChildScrollView(
                                      scrollDirection: Axis.horizontal,
                                      child: Row(
                                        mainAxisAlignment: MainAxisAlignment.end,
                                        children: [
                                          TextButton.icon(
                                            onPressed: () => _showOrderDetails(context, order.id),
                                            icon: const Icon(Icons.info_outline, size: 18),
                                            label: const Text('Ver Detalles'),
                                          ),
                                          if (canEdit)
                                            TextButton.icon(
                                              onPressed: () => _editOrder(context, order),
                                              icon: const Icon(Icons.edit_outlined, size: 18, color: Colors.blue),
                                              label: const Text('Editar', style: TextStyle(color: Colors.blue)),
                                            ),
                                          if (canCancel)
                                            TextButton.icon(
                                              onPressed: () => _cancelOrderDialog(context, order.id),
                                              icon: const Icon(Icons.cancel_outlined, size: 18, color: Colors.red),
                                              label: const Text('Sol. Anulación', style: TextStyle(color: Colors.red)),
                                            ),
                                        ],
                                      ),
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
    );
  }
}
