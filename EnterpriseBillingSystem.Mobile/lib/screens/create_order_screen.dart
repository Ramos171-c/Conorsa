import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/order_provider.dart';
import '../models/product.dart';

class CreateOrderScreen extends StatefulWidget {
  const CreateOrderScreen({super.key});

  @override
  State<CreateOrderScreen> createState() => _CreateOrderScreenState();
}

class _CreateOrderScreenState extends State<CreateOrderScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final _customerSearchController = TextEditingController();
  final _productSearchController = TextEditingController();
  final _notesController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    
    // Load initial data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = Provider.of<OrderProvider>(context, listen: false);
      provider.clearDraft();
      provider.fetchCustomers();
      provider.fetchProducts();
    });

    _customerSearchController.addListener(_onCustomerSearchChanged);
    _productSearchController.addListener(_onProductSearchChanged);
    _notesController.addListener(_onNotesChanged);
  }

  @override
  void dispose() {
    _customerSearchController.dispose();
    _productSearchController.dispose();
    _notesController.dispose();
    _tabController.dispose();
    super.dispose();
  }

  void _onCustomerSearchChanged() {
    final search = _customerSearchController.text.trim();
    Provider.of<OrderProvider>(context, listen: false).fetchCustomers(search: search);
  }

  void _onProductSearchChanged() {
    final search = _productSearchController.text.trim();
    Provider.of<OrderProvider>(context, listen: false).fetchProducts(search: search);
  }

  void _onNotesChanged() {
    Provider.of<OrderProvider>(context, listen: false).setNotes(_notesController.text);
  }

  // Show Product Presentation & Qty selector dialog
  void _showAddProductDialog(Product product) {
    if (product.presentations.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Este producto no tiene presentaciones configuradas.'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    // Default select first presentation
    ProductPresentation selectedPresentation = product.presentations.first;
    double quantity = 1.0;
    final qtyController = TextEditingController(text: '1');

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            final price = selectedPresentation.retailPrice > 0 
                ? selectedPresentation.retailPrice 
                : product.defaultSalePrice;
            final subtotal = price * quantity;

            return AlertDialog(
              title: Text('Agregar ${product.name}'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text('Presentación:', style: TextStyle(fontWeight: FontWeight.bold)),
                  DropdownButton<ProductPresentation>(
                    value: selectedPresentation,
                    isExpanded: true,
                    items: product.presentations.map((p) {
                      final pPrice = p.retailPrice > 0 ? p.retailPrice : product.defaultSalePrice;
                      return DropdownMenuItem(
                        value: p,
                        child: Text('${p.name} - \$${pPrice.toStringAsFixed(2)}'),
                      );
                    }).toList(),
                    onChanged: (val) {
                      if (val != null) {
                        setDialogState(() {
                          selectedPresentation = val;
                        });
                      }
                    },
                  ),
                  const SizedBox(height: 16),
                  const Text('Cantidad:', style: TextStyle(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      IconButton(
                        icon: const Icon(Icons.remove_circle_outline),
                        onPressed: () {
                          if (quantity > 1) {
                            setDialogState(() {
                              quantity -= 1.0;
                              qtyController.text = quantity.toInt().toString();
                            });
                          }
                        },
                      ),
                      Expanded(
                        child: TextField(
                          controller: qtyController,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          textAlign: TextAlign.center,
                          decoration: const InputDecoration(
                            contentPadding: EdgeInsets.zero,
                            border: OutlineInputBorder(),
                          ),
                          onChanged: (val) {
                            final parsed = double.tryParse(val);
                            if (parsed != null && parsed > 0) {
                              setDialogState(() {
                                quantity = parsed;
                              });
                            }
                          },
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.add_circle_outline),
                        onPressed: () {
                          setDialogState(() {
                            quantity += 1.0;
                            qtyController.text = quantity.toInt().toString();
                          });
                        },
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),
                  const Divider(),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Total Estimado:',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                      ),
                      Text(
                        '\$${subtotal.toStringAsFixed(2)}',
                        style: const TextStyle(
                          fontWeight: FontWeight.bold, 
                          fontSize: 18, 
                          color: Color(0xFF1E3A8A),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Cancelar'),
                ),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF0F172A)),
                  onPressed: () {
                    final provider = Provider.of<OrderProvider>(context, listen: false);
                    provider.addToCart(product, selectedPresentation, quantity);
                    Navigator.pop(context);
                    
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text('Agregado: ${product.name} (${selectedPresentation.name}) x${qtyController.text}'),
                        backgroundColor: Colors.teal,
                        duration: const Duration(seconds: 2),
                      ),
                    );
                  },
                  child: const Text('Agregar', style: TextStyle(color: Colors.white)),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _submitOrder(OrderProvider provider) async {
    final success = await provider.submitOrder();
    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Pedido enviado con éxito.'),
          backgroundColor: Colors.teal,
        ),
      );
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = Provider.of<OrderProvider>(context);

    // Watch cart state to see if customer is selected
    final draft = provider.draftOrder;
    final hasCustomer = draft.customerId != null;

    // Handle background error messages in UI
    if (provider.errorMessage != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        showDialog(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Error'),
            content: Text(provider.errorMessage!),
            actions: [
              TextButton(
                onPressed: () {
                  provider.clearMessages();
                  Navigator.pop(context);
                },
                child: const Text('Aceptar'),
              ),
            ],
          ),
        );
      });
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Crear Pedido'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        bottom: TabBar(
          controller: _tabController,
          labelColor: const Color(0xFF38BDF8),
          unselectedLabelColor: Colors.white60,
          indicatorColor: const Color(0xFF38BDF8),
          tabs: [
            const Tab(icon: Icon(Icons.person), text: '1. Cliente'),
            Tab(
              icon: const Icon(Icons.grid_view_rounded), 
              text: '2. Productos',
              // Disable product selection visually if no customer selected
            ),
            Tab(
              icon: Badge(
                label: Text(draft.details.length.toString()),
                child: const Icon(Icons.shopping_cart),
              ), 
              text: '3. Resumen',
            ),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          // STEP 1: SELECT CUSTOMER
          _buildCustomerStep(provider),

          // STEP 2: SELECT PRODUCTS
          !hasCustomer
              ? _buildRequiredCustomerWarning()
              : _buildProductsStep(provider),

          // STEP 3: CART SUMMARY
          !hasCustomer
              ? _buildRequiredCustomerWarning()
              : _buildSummaryStep(provider),
        ],
      ),
    );
  }

  // UI Step 1: Select Customer
  Widget _buildCustomerStep(OrderProvider provider) {
    return Container(
      color: const Color(0xFFF8FAFC),
      padding: const EdgeInsets.all(16.0),
      child: Column(
        children: [
          TextField(
            controller: _customerSearchController,
            decoration: InputDecoration(
              prefixIcon: const Icon(Icons.search),
              hintText: 'Buscar cliente por nombre o código...',
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              filled: true,
              fillColor: Colors.white,
            ),
          ),
          const SizedBox(height: 16),
          Expanded(
            child: provider.isLoading && provider.customers.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : provider.customers.isEmpty
                    ? const Center(child: Text('No se encontraron clientes.'))
                    : ListView.builder(
                        itemCount: provider.customers.length,
                        itemBuilder: (context, index) {
                          final customer = provider.customers[index];
                          final isSelected = provider.draftOrder.customerId == customer.id;

                          return Card(
                            color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                              side: BorderSide(
                                color: isSelected 
                                    ? const Color(0xFF3B82F6) 
                                    : const Color(0xFFE2E8F0),
                                width: isSelected ? 2 : 1,
                              ),
                            ),
                            margin: const EdgeInsets.only(bottom: 12),
                            child: ListTile(
                              leading: CircleAvatar(
                                backgroundColor: customer.isActive 
                                    ? const Color(0xFFDCFCE7) 
                                    : const Color(0xFFFEE2E2),
                                child: Icon(
                                  Icons.person, 
                                  color: customer.isActive ? Colors.green : Colors.red,
                                ),
                              ),
                              title: Text(
                                customer.name,
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('Código: ${customer.customerCode}'),
                                  Text(
                                    'Estado: ${customer.statusText}',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w600,
                                      color: customer.isActive ? Colors.green : Colors.red,
                                    ),
                                  ),
                                  if (customer.defaultDiscountPercentage > 0)
                                    Text('Desc. Predeterminado: ${customer.defaultDiscountPercentage}%'),
                                  if (customer.isTaxExempt)
                                    const Text('Exento de impuestos', style: TextStyle(color: Colors.blue, fontWeight: FontWeight.bold)),
                                ],
                              ),
                              trailing: isSelected 
                                  ? const Icon(Icons.check_circle, color: Color(0xFF3B82F6))
                                  : null,
                              onTap: () {
                                if (customer.isBlocked || customer.isInactive) {
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    SnackBar(
                                      content: Text('El cliente ${customer.name} está bloqueado o inactivo.'),
                                      backgroundColor: Colors.red,
                                    ),
                                  );
                                  return;
                                }
                                provider.setCustomer(customer);
                                // Auto slide to next tab
                                _tabController.animateTo(1);
                              },
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }

  // Warning when customer not selected
  Widget _buildRequiredCustomerWarning() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.person_outline, size: 80, color: Colors.grey.shade300),
            const SizedBox(height: 16),
            Text(
              'Cliente Requerido',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.grey.shade700),
            ),
            const SizedBox(height: 8),
            Text(
              'Seleccione primero un cliente en la pestaña "1. Cliente" para habilitar los productos y el resumen.',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey.shade500),
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: () {
                _tabController.animateTo(0);
              },
              style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF0F172A)),
              child: const Text('Ir a Clientes', style: TextStyle(color: Colors.white)),
            ),
          ],
        ),
      ),
    );
  }

  // UI Step 2: Select Products
  Widget _buildProductsStep(OrderProvider provider) {
    return Container(
      color: const Color(0xFFF8FAFC),
      padding: const EdgeInsets.all(16.0),
      child: Column(
        children: [
          TextField(
            controller: _productSearchController,
            decoration: InputDecoration(
              prefixIcon: const Icon(Icons.search),
              hintText: 'Buscar producto por nombre o código...',
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              filled: true,
              fillColor: Colors.white,
            ),
          ),
          const SizedBox(height: 16),
          Expanded(
            child: provider.isLoading && provider.products.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : provider.products.isEmpty
                    ? const Center(child: Text('No se encontraron productos.'))
                    : ListView.builder(
                        itemCount: provider.products.length,
                        itemBuilder: (context, index) {
                          final product = provider.products[index];

                          return Card(
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                              side: const BorderSide(color: Color(0xFFE2E8F0)),
                            ),
                            margin: const EdgeInsets.only(bottom: 12),
                            child: ListTile(
                              leading: Container(
                                width: 44,
                                height: 44,
                                decoration: BoxDecoration(
                                  color: const Color(0xFFF1F5F9),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Icon(Icons.image_not_supported_outlined, color: Colors.grey.shade400),
                              ),
                              title: Text(
                                product.name,
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('SKU: ${product.internalCode}'),
                                  Text('Precio base: \$${product.defaultSalePrice.toStringAsFixed(2)}'),
                                  Text('Presentaciones: ${product.presentations.length}'),
                                ],
                              ),
                              trailing: IconButton(
                                icon: const Icon(Icons.add_circle, color: Color(0xFF0F172A), size: 28),
                                onPressed: () => _showAddProductDialog(product),
                              ),
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }

  // UI Step 3: Cart Summary
  Widget _buildSummaryStep(OrderProvider provider) {
    final draft = provider.draftOrder;

    return Container(
      color: const Color(0xFFF8FAFC),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Customer info banner
          Container(
            color: const Color(0xFFEFF6FF),
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
            child: Row(
              children: [
                const Icon(Icons.person, color: Colors.blue),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Cliente seleccionado:',
                        style: TextStyle(fontSize: 12, color: Colors.blueGrey, fontWeight: FontWeight.bold),
                      ),
                      Text(
                        draft.customerName ?? 'N/A',
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Color(0xFF1E3A8A)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // Cart Items List
          Expanded(
            child: draft.details.isEmpty
                ? const Center(
                    child: Padding(
                      padding: EdgeInsets.all(32.0),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.shopping_cart_outlined, size: 70, color: const Color(0xFFCBD5E1)),
                          const SizedBox(height: 12),
                          Text(
                            'Su carrito está vacío',
                            style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: const Color(0xFF475569)),
                          ),
                          const SizedBox(height: 6),
                          Text(
                            'Agregue productos desde la pestaña "2. Productos" para armar el pedido.',
                            textAlign: TextAlign.center,
                            style: TextStyle(fontSize: 13, color: const Color(0xFF64748B)),
                          ),
                        ],
                      ),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16.0),
                    itemCount: draft.details.length,
                    itemBuilder: (context, index) {
                      final item = draft.details[index];

                      return Card(
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        margin: const EdgeInsets.only(bottom: 12),
                        child: Padding(
                          padding: const EdgeInsets.all(12.0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Expanded(
                                    child: Text(
                                      item.productName,
                                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                                    ),
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.delete_outline, color: Colors.red),
                                    onPressed: () {
                                      provider.removeFromCart(item.productId, item.unitOfMeasureId);
                                    },
                                  ),
                                ],
                              ),
                              Text('Presentación: ${item.unitOfMeasureCode}'),
                              const SizedBox(height: 8),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  // Quantity modifier
                                  Row(
                                    children: [
                                      IconButton(
                                        icon: const Icon(Icons.remove, size: 20),
                                        onPressed: () {
                                          provider.updateQuantity(
                                            item.productId,
                                            item.unitOfMeasureId,
                                            item.quantity - 1,
                                          );
                                        },
                                      ),
                                      Container(
                                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                                        decoration: BoxDecoration(
                                          border: Border.all(color: Colors.grey.shade300),
                                          borderRadius: BorderRadius.circular(4),
                                        ),
                                        child: Text(
                                          item.quantity.toInt().toString(),
                                          style: const TextStyle(fontWeight: FontWeight.bold),
                                        ),
                                      ),
                                      IconButton(
                                        icon: const Icon(Icons.add, size: 20),
                                        onPressed: () {
                                          provider.updateQuantity(
                                            item.productId,
                                            item.unitOfMeasureId,
                                            item.quantity + 1,
                                          );
                                        },
                                      ),
                                    ],
                                  ),
                                  Column(
                                    crossAxisAlignment: CrossAxisAlignment.end,
                                    children: [
                                      Text(
                                        'Precio: \$${item.unitPrice.toStringAsFixed(2)}',
                                        style: TextStyle(fontSize: 13, color: Colors.grey.shade500),
                                      ),
                                      if (item.discountPercentage > 0)
                                        Text(
                                          'Desc: ${item.discountPercentage}%',
                                          style: const TextStyle(fontSize: 12, color: Colors.green, fontWeight: FontWeight.bold),
                                        ),
                                      Text(
                                        'Neto: \$${item.netAmount.toStringAsFixed(2)}',
                                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF1E293B)),
                                      ),
                                    ],
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

          // Bottom calculations & Notes input
          Container(
            decoration: const BoxDecoration(
              color: Colors.white,
              border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
            ),
            padding: const EdgeInsets.all(20.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextField(
                  controller: _notesController,
                  decoration: const InputDecoration(
                    labelText: 'Notas del pedido (opcional)',
                    border: OutlineInputBorder(),
                    contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  ),
                  maxLines: 2,
                ),
                const SizedBox(height: 16),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Subtotal:'),
                    Text('\$${draft.subTotal.toStringAsFixed(2)}'),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Descuento:'),
                    Text(
                      '-\$${draft.discountAmount.toStringAsFixed(2)}',
                      style: const TextStyle(color: Colors.green),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Impuesto:'),
                    Text(
                      draft.isCustomerTaxExempt 
                          ? 'Exento (\$0.00)' 
                          : '\$${draft.taxAmount.toStringAsFixed(2)}',
                    ),
                  ],
                ),
                const Divider(),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'TOTAL:',
                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: Color(0xFF0F172A)),
                    ),
                    Text(
                      '\$${draft.totalAmount.toStringAsFixed(2)}',
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 20, color: Color(0xFF1E3A8A)),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: provider.isLoading || draft.details.isEmpty
                      ? null
                      : () => _submitOrder(provider),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F172A),
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: provider.isLoading
                      ? const CircularProgressIndicator(color: Colors.white)
                      : const Text(
                          'Confirmar y Enviar Pedido',
                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
                        ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
