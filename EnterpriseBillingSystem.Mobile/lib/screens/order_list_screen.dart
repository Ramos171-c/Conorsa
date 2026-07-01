import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/order_provider.dart';
import '../providers/pos_provider.dart';
import '../models/order.dart';
import 'pos_screen.dart';

class OrderListScreen extends StatefulWidget {
  const OrderListScreen({super.key});

  @override
  State<OrderListScreen> createState() => _OrderListScreenState();
}

class _OrderListScreenState extends State<OrderListScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<OrderProvider>(context, listen: false).fetchOrders();
    });
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
          title: const Text('Anular Pedido'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('¿Está seguro de que desea anular este pedido?'),
              const SizedBox(height: 12),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(
                  labelText: 'Motivo de la anulación',
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
                    const SnackBar(content: Text('Debe ingresar un motivo de anulación.')),
                  );
                  return;
                }
                Navigator.pop(context);
                final orderProv = Provider.of<OrderProvider>(context, listen: false);
                final success = await orderProv.cancelOrder(orderId, reason);
                if (success) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Pedido anulado con éxito.')),
                  );
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(orderProv.errorMessage ?? 'Error al anular el pedido.')),
                  );
                }
              },
              child: const Text('Anular', style: TextStyle(color: Colors.red)),
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
      
      final detail = await orderProv.fetchOrderDetail(orderItem.id);
      Navigator.pop(context); // Close loading dialog

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
      Navigator.pop(context); // Close loading dialog if open
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error al editar: $e')),
      );
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
          height: MediaQuery.of(context).size.height * 0.75,
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
      ),
      body: RefreshIndicator(
        onRefresh: () => provider.fetchOrders(),
        child: provider.isLoading && provider.orders.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : provider.orders.isEmpty
                ? const Center(
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
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: provider.orders.length,
                    itemBuilder: (context, index) {
                      final order = provider.orders[index];
                      final statusColor = _getStatusColor(order.status);
                      final formattedDate = '${order.orderDate.day}/${order.orderDate.month}/${order.orderDate.year}';

                      // 10 minute rule for editing
                      final difference = DateTime.now().difference(order.orderDate.toLocal());
                      final canCancel = order.status.toLowerCase() != 'cancelled' &&
                          order.status.toLowerCase() != 'anulado' &&
                          order.status.toLowerCase() != 'completed' &&
                          order.status.toLowerCase() != 'completado';
                      final canEdit = difference.inMinutes < 10 && canCancel;

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
                              Row(
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
                                      label: const Text('Anular', style: TextStyle(color: Colors.red)),
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
    );
  }
}
