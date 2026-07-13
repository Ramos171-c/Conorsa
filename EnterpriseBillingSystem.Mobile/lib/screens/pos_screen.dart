import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/order_provider.dart';
import '../providers/pos_provider.dart';
import '../models/product.dart';
import '../models/customer.dart';
import '../widgets/cached_product_image.dart';

class PosScreen extends StatefulWidget {
  const PosScreen({super.key});

  @override
  State<PosScreen> createState() => _PosScreenState();
}

class _PosScreenState extends State<PosScreen> {
  final _productSearchController = TextEditingController();
  final _customerSearchController = TextEditingController();
  final _exchangeRateController = TextEditingController();
  Timer? _debounce;
  int _activeTab = 0;
  bool _showOnlyTopProducts = false;
  String? _selectedProductCategoryId;

  @override
  void initState() {
    super.initState();
    
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      final posProv = Provider.of<PosProvider>(context, listen: false);
      final orderProv = Provider.of<OrderProvider>(context, listen: false);

      if (posProv.editingOrderId == null) {
        posProv.clearCart();
        
        // Load default customer (CONSUMIDOR FINAL / CUS-000001)
        await orderProv.fetchCustomers(search: 'CUS-000001');
        if (orderProv.customers.isNotEmpty) {
          final c = orderProv.customers.first;
          posProv.setCustomer(c);
          await orderProv.fetchCustomerTopProducts(c.id);
        } else {
          // Fallback generic customer
          final c = Customer(
            id: '00000000-0000-0000-0000-000000000000',
            customerCode: 'CUS-000001',
            name: 'CONSUMIDOR FINAL',
            isTaxExempt: false,
            defaultDiscountPercentage: 0.0,
            status: 1,
            canUseCredit: false,
            creditLimit: 0.0,
            creditDays: 0,
            customerPricingProfileType: 0,
            currentDebt: 0.0,
          );
          posProv.setCustomer(c);
          await orderProv.fetchCustomerTopProducts(c.id);
        }
      } else {
        if (posProv.selectedCustomer != null) {
          await orderProv.fetchCustomerTopProducts(posProv.selectedCustomer!.id);
        }
      }
      
      // Load active thresholds and currencies dynamically
      await posProv.fetchConfigurations();

      // Initial product fetch
      await orderProv.fetchProducts();
      await orderProv.fetchProductCategories();
      await orderProv.fetchSystemParameters();

      _exchangeRateController.text = posProv.exchangeRate.toString();
    });

    _productSearchController.addListener(_onProductSearchChanged);
  }

  @override
  void dispose() {
    _productSearchController.dispose();
    _customerSearchController.dispose();
    _exchangeRateController.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onProductSearchChanged() {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      final search = _productSearchController.text.trim();
      Provider.of<OrderProvider>(context, listen: false).fetchProducts(search: search);
    });
  }

  // Show presentation & qty dialog with manual price option
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

    final posProv = Provider.of<PosProvider>(context, listen: false);
    ProductPresentation selectedPresentation = product.presentations.firstWhere(
      (p) => p.isDefaultSalePresentation,
      orElse: () => product.presentations.first,
    );
    double quantity = 1.0;
    final qtyController = TextEditingController(text: '1');

    // Calculate initial price based on level
    double initialPrice = selectedPresentation.retailPrice;
    if (posProv.currentLevel == 'MAYORISTA') {
      initialPrice = selectedPresentation.wholesalePrice > 0 ? selectedPresentation.wholesalePrice : selectedPresentation.retailPrice;
    } else if (posProv.currentLevel == 'SEMI MAYORISTA') {
      initialPrice = selectedPresentation.semiWholesalePrice > 0 ? selectedPresentation.semiWholesalePrice : selectedPresentation.retailPrice;
    }
    final priceController = TextEditingController(text: initialPrice.toStringAsFixed(2));

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            // Read user manual price
            final userPrice = double.tryParse(priceController.text) ?? 0.0;
            final subtotal = userPrice * quantity;

            return AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              title: Text('Agregar ${product.name}', style: const TextStyle(fontWeight: FontWeight.bold)),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                  const Text('Presentación:', style: TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF64748B))),
                  const SizedBox(height: 6),
                  DropdownButtonFormField<ProductPresentation>(
                    value: selectedPresentation,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      border: OutlineInputBorder(),
                    ),
                    items: product.presentations.map((p) {
                      final conversionText = p.conversionFactor > 1 
                          ? ' (${p.conversionFactor.toInt()} ${p.unitOfMeasureCode})' 
                          : ' (1 ${p.unitOfMeasureCode})';
                      return DropdownMenuItem(
                        value: p,
                        child: Text('${p.name}$conversionText'),
                      );
                    }).toList(),
                    onChanged: (val) {
                      if (val != null) {
                        setDialogState(() {
                          selectedPresentation = val;
                          
                          // Recalculate price
                          double newPrice = selectedPresentation.retailPrice;
                          if (posProv.currentLevel == 'MAYORISTA') {
                            newPrice = selectedPresentation.wholesalePrice > 0 ? selectedPresentation.wholesalePrice : selectedPresentation.retailPrice;
                          } else if (posProv.currentLevel == 'SEMI MAYORISTA') {
                            newPrice = selectedPresentation.semiWholesalePrice > 0 ? selectedPresentation.semiWholesalePrice : selectedPresentation.retailPrice;
                          }
                          priceController.text = newPrice.toStringAsFixed(2);
                        });
                      }
                    },
                  ),
                  const SizedBox(height: 12),

                  // Unit Price Input
                  const Text('Precio Unitario:', style: TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF64748B))),
                  const SizedBox(height: 6),
                  TextField(
                    controller: priceController,
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    decoration: const InputDecoration(
                      contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      border: OutlineInputBorder(),
                      prefixText: 'C\$ ',
                    ),
                    onChanged: (val) {
                      setDialogState(() {}); // rebuilds dialog to update subtotal live!
                    },
                  ),
                  const SizedBox(height: 12),

                  const Text('Cantidad:', style: TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF64748B))),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      IconButton(
                        icon: const Icon(Icons.remove_circle_outline, size: 28),
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
                            contentPadding: EdgeInsets.symmetric(vertical: 8),
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
                        icon: const Icon(Icons.add_circle_outline, size: 28),
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
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Total estimado:', style: TextStyle(fontWeight: FontWeight.bold)),
                        Text(
                          'C\$ ${subtotal.toStringAsFixed(2)}',
                          style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF0F172A), fontSize: 16),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Cancelar'),
                ),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F172A),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  ),
                  onPressed: () {
                    final manualPrice = double.tryParse(priceController.text);
                    posProv.addToCart(product, selectedPresentation, quantity, manualPrice: manualPrice);
                    Navigator.pop(context);
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text('Agregado ${product.name} al carrito'),
                        duration: const Duration(seconds: 1),
                      ),
                    );
                  },
                  child: const Text('Agregar'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  // Show cart item edit modal (quantity, manual price)
  void _showEditCartItemDialog(BuildContext context, PosProvider posProv, CartItem item) {
    final priceController = TextEditingController(text: item.unitPriceDisplayed.toStringAsFixed(2));
    final qtyController = TextEditingController(text: item.quantity.toStringAsFixed(0));

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: Text('Ajustar ${item.product.name}', style: const TextStyle(fontWeight: FontWeight.bold)),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
              Text(
                'Presentación: ${item.presentation.name} (${item.presentation.conversionFactor.toInt()} ${item.presentation.unitOfMeasureCode})', 
                style: const TextStyle(color: Color(0xFF64748B), fontSize: 13)
              ),
              const SizedBox(height: 12),
              
              // Quantity Input
              const Text('Cantidad:', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13, color: Color(0xFF1E293B))),
              const SizedBox(height: 4),
              TextField(
                controller: qtyController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  contentPadding: EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),

              // Unit Price Override Input
              const Text('Precio Unitario (Manual):', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13, color: Color(0xFF1E293B))),
              const SizedBox(height: 4),
              TextField(
                controller: priceController,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  contentPadding: EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                  border: OutlineInputBorder(),
                  prefixText: 'C\$ ',
                ),
              ),
            ],
          ),
        ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancelar'),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF0F172A),
                foregroundColor: Colors.white,
              ),
              onPressed: () {
                final qty = double.tryParse(qtyController.text) ?? item.quantity;
                final price = double.tryParse(priceController.text) ?? item.unitPriceDisplayed;

                posProv.updateCartItemManualDetails(item, qty, price);
                Navigator.pop(context);
              },
              child: const Text('Guardar'),
            ),
          ],
        );
      },
    );
  }

  // Show customer selection modal
  void _showCustomerSelectionDialog() {
    final orderProv = Provider.of<OrderProvider>(context, listen: false);
    final posProv = Provider.of<PosProvider>(context, listen: false);
    final authProv = Provider.of<AuthProvider>(context, listen: false);

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              title: const Text('Seleccionar Cliente', style: TextStyle(fontWeight: FontWeight.bold)),
              content: SizedBox(
                width: double.maxFinite,
                height: 400,
                child: Column(
                  children: [
                    TextField(
                      controller: _customerSearchController,
                      decoration: const InputDecoration(
                        labelText: 'Buscar por nombre, código o teléfono',
                        prefixIcon: Icon(Icons.search),
                        border: OutlineInputBorder(),
                      ),
                      onChanged: (val) async {
                        await orderProv.fetchCustomers(search: val, routeId: authProv.userProfile?.effectiveRouteId);
                        setDialogState(() {});
                      },
                    ),
                    const SizedBox(height: 12),
                    Expanded(
                      child: orderProv.isLoading
                          ? const Center(child: CircularProgressIndicator())
                          : ListView.builder(
                              itemCount: orderProv.customers.length + 1,
                              itemBuilder: (context, index) {
                                if (index == 0) {
                                  // Generic Consumer Final option
                                  return ListTile(
                                    leading: const CircleAvatar(child: Icon(Icons.person_outline)),
                                    title: const Text('CONSUMIDOR FINAL (CUS-000001)'),
                                    subtitle: const Text('Nivel Base: Detalle'),
                                    onTap: () {
                                      final fallback = Customer(
                                        id: '00000000-0000-0000-0000-000000000000',
                                        customerCode: 'CUS-000001',
                                        name: 'CONSUMIDOR FINAL',
                                        isTaxExempt: false,
                                        defaultDiscountPercentage: 0.0,
                                        status: 1,
                                        canUseCredit: false,
                                        creditLimit: 0.0,
                                        creditDays: 0,
                                        customerPricingProfileType: 0,
                                        currentDebt: 0.0,
                                      );
                                      setState(() {
                                        _showOnlyTopProducts = false;
                                      });
                                      posProv.setCustomer(fallback);
                                      orderProv.fetchCustomerTopProducts(fallback.id);
                                      Navigator.pop(context);
                                    },
                                  );
                                }
                                
                                final customer = orderProv.customers[index - 1];
                                String levelName = 'Detalle';
                                if (customer.customerPricingProfileType == 2) {
                                  levelName = 'Mayorista';
                                } else if (customer.customerPricingProfileType == 1) {
                                  levelName = 'Semi Mayorista';
                                }

                                return ListTile(
                                  leading: const CircleAvatar(child: Icon(Icons.person)),
                                  title: Text(customer.name),
                                  subtitle: Text('Código: ${customer.customerCode} | Nivel Base: $levelName'),
                                  trailing: customer.canUseCredit 
                                      ? const Chip(label: Text('Crédito', style: TextStyle(fontSize: 10)), backgroundColor: Colors.greenAccent)
                                      : null,
                                  onTap: () {
                                    setState(() {
                                      _showOnlyTopProducts = false;
                                    });
                                    posProv.setCustomer(customer);
                                    orderProv.fetchCustomerTopProducts(customer.id);
                                    Navigator.pop(context);
                                  },
                                );
                              },
                            ),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Cerrar'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  // Show checkout dialog
  void _showCheckoutDialog() {
    final posProv = Provider.of<PosProvider>(context, listen: false);
    if (posProv.cart.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('El carrito está vacío'), backgroundColor: Colors.red),
      );
      return;
    }

    final notesController = TextEditingController(text: 'Pedido desde POS Móvil (Vendedor)');
    String? localError;

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              title: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Confirmar Pedido', style: TextStyle(fontWeight: FontWeight.bold)),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Invoice total overview
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: const Color(0xFF0F172A),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Column(
                        children: [
                          const Text('TOTAL PEDIDO', style: TextStyle(color: Colors.white70, fontSize: 12, fontWeight: FontWeight.w600)),
                          const SizedBox(height: 4),
                          Text(
                            'C\$ ${posProv.subtotalCommercial.toStringAsFixed(2)}',
                            style: const TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '\$ ${posProv.usdTotal.toStringAsFixed(2)} USD',
                            style: const TextStyle(color: Color(0xFF38BDF8), fontSize: 14, fontWeight: FontWeight.w600),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'Cliente: ${posProv.selectedCustomer?.name ?? "CONSUMIDOR FINAL"}',
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF1E293B)),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: notesController,
                      maxLines: 3,
                      decoration: const InputDecoration(
                        labelText: 'Notas / Observaciones',
                        hintText: 'Ingrese alguna nota para el pedido...',
                        border: OutlineInputBorder(),
                        alignLabelWithHint: true,
                      ),
                    ),
                    if (localError != null) ...[
                      const SizedBox(height: 12),
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.red.shade50,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.red.shade200),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(Icons.error_outline_rounded, color: Colors.red, size: 20),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                localError!,
                                style: const TextStyle(color: Colors.red, fontSize: 13, fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Cancelar'),
                ),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F172A),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  ),
                  onPressed: posProv.isBusy
                      ? null
                      : () async {
                          setDialogState(() {
                            localError = null;
                          });
                          
                          final success = await posProv.checkout(
                            notes: notesController.text.trim(),
                          );

                          if (success) {
                            if (!context.mounted) return;
                            Navigator.pop(context); // Close checkout
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text(posProv.successMessage ?? 'Pedido registrado con éxito.'),
                                backgroundColor: Colors.green,
                              ),
                            );
                          } else {
                            if (!context.mounted) return;
                            setDialogState(() {
                              localError = posProv.errorMessage ?? 'Ocurrió un error al procesar el pedido.';
                            });
                          }
                        },
                  child: posProv.isBusy
                      ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                      : const Text('Confirmar Pedido'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Widget _buildHeaderPanel(PosProvider posProv, AuthProvider auth) {
    Color badgeColor = Colors.green;
    IconData badgeIcon = Icons.star_border_rounded;
    if (posProv.currentLevel == 'MAYORISTA') {
      badgeColor = Colors.blue.shade700;
      badgeIcon = Icons.stars_rounded;
    } else if (posProv.currentLevel == 'SEMI MAYORISTA') {
      badgeColor = Colors.orange.shade700;
      badgeIcon = Icons.star_half_rounded;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: Color(0xFFE2E8F0))),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Row(
                  children: [
                    const Icon(Icons.person_outline_rounded, color: Color(0xFF64748B), size: 18),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Cliente: ${posProv.selectedCustomer?.name ?? 'CONSUMIDOR FINAL'}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Color(0xFF1E293B)),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              PopupMenuButton<String>(
                tooltip: 'Cambiar Nivel de Precios',
                onSelected: (level) {
                  posProv.setManualPricingLevelOverride(level);
                },
                itemBuilder: (context) => [
                  const PopupMenuItem(
                    value: 'DETALLE',
                    child: Row(
                      children: [
                        Icon(Icons.star_border_rounded, color: Colors.green),
                        SizedBox(width: 8),
                        Text('Detalle'),
                      ],
                    ),
                  ),
                  const PopupMenuItem(
                    value: 'SEMI MAYORISTA',
                    child: Row(
                      children: [
                        Icon(Icons.star_half_rounded, color: Colors.orange),
                        SizedBox(width: 8),
                        Text('Semi Mayorista'),
                      ],
                    ),
                  ),
                  const PopupMenuItem(
                    value: 'MAYORISTA',
                    child: Row(
                      children: [
                        Icon(Icons.stars_rounded, color: Colors.blue),
                        SizedBox(width: 8),
                        Text('Mayorista'),
                      ],
                    ),
                  ),
                ],
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: badgeColor.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: badgeColor, width: 1.2),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(badgeIcon, color: badgeColor, size: 13),
                      const SizedBox(width: 4),
                      Text(
                        posProv.currentLevel,
                        style: TextStyle(color: badgeColor, fontWeight: FontWeight.bold, fontSize: 10),
                      ),
                      const SizedBox(width: 2),
                      Icon(Icons.arrow_drop_down_rounded, color: badgeColor, size: 14),
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Row(
                  children: [
                    const Icon(Icons.badge_outlined, color: Color(0xFF64748B), size: 16),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Vendedor: ${auth.userProfile?.fullName ?? 'N/A'}',
                        style: const TextStyle(fontSize: 12, color: Color(0xFF64748B), fontWeight: FontWeight.w600),
                        overflow: TextOverflow.ellipsis,
                        maxLines: 1,
                      ),
                    ),
                  ],
                ),
              ),
              Row(
                children: [
                  const Text('T/C: C\$ ', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF1E293B))),
                  SizedBox(
                    width: 50,
                    height: 22,
                    child: TextField(
                      controller: _exchangeRateController,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                      decoration: const InputDecoration(
                        contentPadding: EdgeInsets.symmetric(horizontal: 4, vertical: 2),
                        isDense: true,
                        border: OutlineInputBorder(),
                      ),
                      onChanged: (val) {
                        final parsed = double.tryParse(val);
                        if (parsed != null && parsed > 0) {
                          posProv.setExchangeRate(parsed);
                        }
                      },
                    ),
                  ),
                  const Text(r' ↔ $1 USD', style: TextStyle(fontSize: 11, color: Color(0xFF64748B), fontWeight: FontWeight.w600)),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final posProv = Provider.of<PosProvider>(context);
    final orderProv = Provider.of<OrderProvider>(context);
    final auth = Provider.of<AuthProvider>(context);

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: Text(posProv.editingOrderId != null ? 'Editar Pedido' : 'Crear Pedido'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.person_search_rounded),
            tooltip: 'Seleccionar Cliente',
            onPressed: _showCustomerSelectionDialog,
          ),
          IconButton(
            icon: const Icon(Icons.delete_sweep_rounded),
            tooltip: 'Limpiar Carrito',
            onPressed: () {
              if (posProv.cart.isNotEmpty) {
                showDialog(
                  context: context,
                  builder: (context) => AlertDialog(
                    title: const Text('Limpiar Venta'),
                    content: const Text('¿Está seguro de que desea limpiar todos los productos del carrito?'),
                    actions: [
                      TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
                      TextButton(
                        onPressed: () {
                          posProv.clearCart();
                          Navigator.pop(context);
                        },
                        child: const Text('Limpiar', style: TextStyle(color: Colors.red)),
                      ),
                    ],
                  ),
                );
              }
            },
          ),
        ],
      ),
      body: Column(
        children: [
          if (posProv.editingOrderId != null)
            Container(
              color: Colors.amber.shade700,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  const Icon(Icons.edit_note_rounded, color: Colors.white),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      'Modo Edición: Modificando un pedido existente',
                      style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                    ),
                  ),
                  TextButton(
                    onPressed: () {
                      posProv.clearCart();
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Edición cancelada.')),
                      );
                    },
                    style: TextButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: Colors.red.shade800,
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      minimumSize: Size.zero,
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                    child: const Text('Cancelar Edición', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                  ),
                ],
              ),
            ),
          _buildHeaderPanel(posProv, auth),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                if (constraints.maxWidth > 720) {
                  // Tablet/Desktop side-by-side view
                  return Row(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        flex: 3,
                        child: _buildCatalogPanel(posProv, orderProv),
                      ),
                      const VerticalDivider(width: 1, color: Color(0xFFE2E8F0)),
                      Expanded(
                        flex: 2,
                        child: _buildCartPanel(posProv, auth),
                      ),
                    ],
                  );
                } else {
                  // Mobile tabbed view
                  return IndexedStack(
                    index: _activeTab,
                    children: [
                      _buildCatalogPanel(posProv, orderProv),
                      _buildCartPanel(posProv, auth),
                    ],
                  );
                }
              },
            ),
          ),
        ],
      ),
      bottomNavigationBar: MediaQuery.of(context).size.width <= 720
          ? BottomNavigationBar(
              currentIndex: _activeTab,
              onTap: (index) {
                setState(() {
                  _activeTab = index;
                });
              },
              selectedItemColor: const Color(0xFF0F172A),
              unselectedItemColor: const Color(0xFF94A3B8),
              items: [
                const BottomNavigationBarItem(
                  icon: Icon(Icons.grid_view_rounded),
                  label: 'Catálogo',
                ),
                BottomNavigationBarItem(
                  icon: Badge(
                    label: Text(posProv.cart.length.toString()),
                    isLabelVisible: posProv.cart.isNotEmpty,
                    child: const Icon(Icons.shopping_cart_rounded),
                  ),
                  label: 'Carrito',
                ),
              ],
            )
          : null,
    );
  }

  Widget _buildCatalogPanel(PosProvider posProv, OrderProvider orderProv) {
    final query = _productSearchController.text.trim().toLowerCase();
    List<Product> displayedProducts = _showOnlyTopProducts
        ? orderProv.topProducts
        : orderProv.products;

    // Filter by selected category if any
    if (_selectedProductCategoryId != null) {
      displayedProducts = displayedProducts.where((p) => p.categoryId == _selectedProductCategoryId).toList();
    }

    if (query.isNotEmpty) {
      displayedProducts = displayedProducts.where((p) =>
          p.name.toLowerCase().contains(query) ||
          p.internalCode.toLowerCase().contains(query)
      ).toList();
    }

    return Padding(
      padding: const EdgeInsets.all(12.0),
      child: Column(
        children: [
          // Search Bar
          TextField(
            controller: _productSearchController,
            decoration: InputDecoration(
              hintText: 'Buscar producto...',
              prefixIcon: const Icon(Icons.search_rounded),
              suffixIcon: _productSearchController.text.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear_rounded),
                      onPressed: () {
                        _productSearchController.clear();
                      },
                    )
                  : null,
              filled: true,
              fillColor: Colors.white,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12),
                borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
              ),
            ),
          ),
          const SizedBox(height: 8),

          // Selection filter chips (Todos vs Frecuentes vs Categories)
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                FilterChip(
                  label: const Text('Todos'),
                  selected: !_showOnlyTopProducts && _selectedProductCategoryId == null,
                  onSelected: (val) {
                    if (val) {
                      setState(() {
                        _showOnlyTopProducts = false;
                        _selectedProductCategoryId = null;
                      });
                    }
                  },
                  selectedColor: const Color(0xFF0F172A).withOpacity(0.15),
                  labelStyle: TextStyle(
                    color: (!_showOnlyTopProducts && _selectedProductCategoryId == null)
                        ? const Color(0xFF0F172A)
                        : const Color(0xFF64748B),
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(width: 8),
                FilterChip(
                  label: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.star_rounded, size: 16, color: Colors.amber),
                      const SizedBox(width: 4),
                      Text('Frecuentes (${orderProv.topProducts.length})'),
                    ],
                  ),
                  selected: _showOnlyTopProducts,
                  onSelected: orderProv.topProducts.isEmpty
                      ? null
                      : (val) {
                          if (val) {
                            setState(() {
                              _showOnlyTopProducts = true;
                              _selectedProductCategoryId = null;
                            });
                          }
                        },
                  selectedColor: const Color(0xFF0F172A).withOpacity(0.15),
                  labelStyle: TextStyle(
                    color: _showOnlyTopProducts ? const Color(0xFF0F172A) : const Color(0xFF64748B),
                    fontWeight: FontWeight.bold,
                  ),
                ),
                ...orderProv.productCategories.map((category) {
                  final catId = category['id'] as String;
                  final catName = category['name'] as String;
                  final isSelected = _selectedProductCategoryId == catId;
                  return Padding(
                    padding: const EdgeInsets.only(left: 8.0),
                    child: FilterChip(
                      label: Text(catName),
                      selected: isSelected,
                      onSelected: (val) {
                        setState(() {
                          _selectedProductCategoryId = val ? catId : null;
                          _showOnlyTopProducts = false;
                        });
                      },
                      selectedColor: const Color(0xFF0F172A).withOpacity(0.15),
                      labelStyle: TextStyle(
                        color: isSelected ? const Color(0xFF0F172A) : const Color(0xFF64748B),
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  );
                }).toList(),
              ],
            ),
          ),
          const SizedBox(height: 12),

          // Catalog Grid
          Expanded(
            child: (orderProv.isLoading && orderProv.products.isEmpty)
                ? const Center(child: CircularProgressIndicator())
                : (orderProv.errorMessage != null && displayedProducts.isEmpty)
                    ? Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24.0),
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              const Icon(Icons.error_outline_rounded, color: Colors.red, size: 48),
                              const SizedBox(height: 12),
                              Text(
                                orderProv.errorMessage!,
                                textAlign: TextAlign.center,
                                style: const TextStyle(color: Colors.red, fontWeight: FontWeight.w600),
                              ),
                              const SizedBox(height: 16),
                              ElevatedButton.icon(
                                onPressed: () {
                                  orderProv.fetchProducts();
                                },
                                icon: const Icon(Icons.refresh_rounded),
                                label: const Text('Reintentar Cargar'),
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: const Color(0xFF0F172A),
                                  foregroundColor: Colors.white,
                                ),
                              ),
                            ],
                          ),
                        ),
                      )
                    : displayedProducts.isEmpty
                        ? const Center(child: Text('No se encontraron productos.'))
                        : GridView.builder(
                        gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                          maxCrossAxisExtent: 180,
                          childAspectRatio: 0.72,
                          crossAxisSpacing: 10,
                          mainAxisSpacing: 10,
                        ),
                        itemCount: displayedProducts.length,
                        itemBuilder: (context, index) {
                          final p = displayedProducts[index];
                          final hasImage = p.imageUrl != null && p.imageUrl!.isNotEmpty;
                          
                          return Card(
                            color: Colors.white,
                            elevation: 0,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                              side: const BorderSide(color: Color(0xFFE2E8F0)),
                            ),
                            child: InkWell(
                              onTap: p.isSoldOut ? null : () => _showAddProductDialog(p),
                              borderRadius: BorderRadius.circular(12),
                              child: Padding(
                                padding: const EdgeInsets.all(8.0),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    // Image / Icon
                                    Expanded(
                                      child: ClipRRect(
                                        borderRadius: BorderRadius.circular(8),
                                        child: CachedProductImage(
                                          imageUrl: p.imageUrl,
                                          productCode: p.internalCode,
                                          width: double.infinity,
                                          fit: BoxFit.cover,
                                          iconSize: 40,
                                        ),
                                      ),
                                    ),
                                    const SizedBox(height: 8),
                                    Text(
                                      p.name,
                                      maxLines: 2,
                                      overflow: TextOverflow.ellipsis,
                                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Color(0xFF1E293B)),
                                    ),
                                    const SizedBox(height: 2),
                                    Text(
                                      p.internalCode,
                                      style: const TextStyle(color: Color(0xFF64748B), fontSize: 11, fontWeight: FontWeight.w600),
                                    ),
                                    const SizedBox(height: 6),
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'C\$ ${_getProductPriceForLevel(p, posProv.currentLevel).toStringAsFixed(2)}',
                                          style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF0F172A), fontSize: 12),
                                        ),
                                        p.isSoldOut
                                            ? const Text('Agotado', style: TextStyle(color: Colors.red, fontSize: 10, fontWeight: FontWeight.bold))
                                            : const Text('Disp', style: TextStyle(color: Colors.green, fontSize: 10, fontWeight: FontWeight.bold)),
                                      ],
                                    ),
                                  ],
                                ),
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

  Widget _buildCartPanel(PosProvider posProv, AuthProvider auth) {
    return Container(
      color: Colors.white,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [

          // Shopping Cart Items list
          Expanded(
            child: posProv.cart.isEmpty
                ? const Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.shopping_cart_outlined, size: 48, color: Colors.grey),
                        SizedBox(height: 8),
                        Text('El carrito está vacío', style: TextStyle(color: Colors.grey, fontSize: 14)),
                      ],
                    ),
                  )
                : ListView.builder(
                    itemCount: posProv.cart.length,
                    itemBuilder: (context, index) {
                      final item = posProv.cart[index];
                      return Card(
                        margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                          side: const BorderSide(color: Color(0xFFE2E8F0)),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 8.0, vertical: 6.0),
                          child: Row(
                            children: [
                              Expanded(
                                child: InkWell(
                                  onTap: () => _showEditCartItemDialog(context, posProv, item),
                                  borderRadius: BorderRadius.circular(4),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          Expanded(
                                            child: Text(
                                              item.product.name,
                                              maxLines: 1,
                                              overflow: TextOverflow.ellipsis,
                                              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12),
                                            ),
                                          ),
                                          const Icon(Icons.edit_rounded, size: 12, color: Color(0xFF94A3B8)),
                                          const SizedBox(width: 4),
                                        ],
                                      ),
                                      Text(
                                        '${item.presentation.name} | C\$ ${item.unitPriceDisplayed.toStringAsFixed(2)}',
                                        style: const TextStyle(color: Color(0xFF64748B), fontSize: 10),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              // Quantity counters
                              Row(
                                children: [
                                  IconButton(
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                    icon: const Icon(Icons.remove, size: 16),
                                    onPressed: () {
                                      posProv.updateQuantity(item, item.quantity - 1);
                                    },
                                  ),
                                  const SizedBox(width: 6),
                                  Text(
                                    item.quantity.toInt().toString(),
                                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12),
                                  ),
                                  const SizedBox(width: 6),
                                  IconButton(
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                    icon: const Icon(Icons.add, size: 16),
                                    onPressed: () {
                                      posProv.updateQuantity(item, item.quantity + 1);
                                    },
                                  ),
                                ],
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'C\$ ${item.lineTotal.toStringAsFixed(2)}',
                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 11),
                              ),
                              IconButton(
                                padding: EdgeInsets.zero,
                                constraints: const BoxConstraints(),
                                icon: const Icon(Icons.delete_outline, color: Colors.red, size: 18),
                                onPressed: () => posProv.removeFromCart(item),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
          ),

          // Progress to Next Level Indicator
          if (posProv.nextLevel.isNotEmpty)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              color: const Color(0xFFF8FAFC),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Progreso a ${posProv.nextLevel}:',
                        style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: Color(0xFF64748B)),
                      ),
                      Text(
                        'Faltan C\$ ${posProv.missingToNextLevel.toStringAsFixed(2)}',
                        style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFFA21CAF)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(4),
                    child: LinearProgressIndicator(
                      value: posProv.progressToNextLevel,
                      minHeight: 4,
                      backgroundColor: Colors.grey.shade200,
                      color: const Color(0xFFA21CAF),
                    ),
                  ),
                ],
              ),
            ),

          // Totals Area
          Container(
            padding: const EdgeInsets.all(12),
            decoration: const BoxDecoration(
              color: Color(0xFFF1F5F9),
              border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
            ),
            child: Column(
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Subtotal Base (Retail):', style: TextStyle(color: Color(0xFF64748B), fontSize: 11)),
                    Text('C\$ ${posProv.subtotalBase.toStringAsFixed(2)}', style: const TextStyle(color: Color(0xFF64748B), fontSize: 11)),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Total Comercial:',
                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF0F172A)),
                    ),
                    Text(
                      'C\$ ${posProv.subtotalCommercial.toStringAsFixed(2)}',
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: Color(0xFF0F172A)),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Equivalente USD:', style: TextStyle(color: Color(0xFF0284C7), fontWeight: FontWeight.w600, fontSize: 12)),
                    Text('\$ ${posProv.usdTotal.toStringAsFixed(2)} USD', style: const TextStyle(color: Color(0xFF0284C7), fontWeight: FontWeight.bold, fontSize: 13)),
                  ],
                ),
                const SizedBox(height: 10),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF0F172A),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: posProv.cart.isEmpty ? null : _showCheckoutDialog,
                    child: const Text('CONFIRMAR PEDIDO', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  double _getProductPriceForLevel(Product product, String level) {
    if (product.presentations.isEmpty) {
      return product.defaultSalePrice;
    }
    final presentation = product.presentations.firstWhere(
      (pres) => pres.isDefaultSalePresentation,
      orElse: () => product.presentations.first,
    );

    if (level == 'MAYORISTA') {
      return presentation.wholesalePrice > 0 
          ? presentation.wholesalePrice 
          : presentation.retailPrice;
    } else if (level == 'SEMI MAYORISTA') {
      return presentation.semiWholesalePrice > 0 
          ? presentation.semiWholesalePrice 
          : presentation.retailPrice;
    } else {
      return presentation.retailPrice;
    }
  }
}

class ImageFit {
  const ImageFit();
  get fit => BoxFit.cover;
}
