import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/product.dart';
import '../providers/auth_provider.dart';
import '../providers/order_provider.dart';
import '../widgets/cached_product_image.dart';

class ProductDetailScreen extends StatelessWidget {
  final Product product;

  const ProductDetailScreen({super.key, required this.product});

  // Presentation selector for logged-in salespeople
  void _showSalespersonAddDialog(BuildContext context) {
    final provider = Provider.of<OrderProvider>(context, listen: false);
    if (product.presentations.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Este producto no tiene presentaciones configuradas.'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

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
              title: Text('Agregar a Pedido: ${product.name}'),
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
                        'Subtotal:',
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
                    provider.addToCart(product, selectedPresentation, quantity);
                    Navigator.pop(context); // Close dialog
                    Navigator.pop(context); // Close detail screen
                    
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

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    final orderProvider = Provider.of<OrderProvider>(context);
    final hasActiveOrder = auth.isLoggedIn && 
        orderProvider.draftOrder.customerId != null && 
        !product.isSoldOut;

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text('Detalle de Producto'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 1. Large Visual Product Image Banner
             CachedProductImage(
              imageUrl: product.imageUrl,
              height: 260,
              width: double.infinity,
              fit: BoxFit.cover,
              iconSize: 100,
            ),
            
            // 2. Info details
            Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Code Badge
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      'Código SKU: ${product.internalCode}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                        color: Color(0xFF475569),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Product Name
                  Text(
                    product.name,
                    style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF0F172A),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Description Section
                  if (product.description != null && product.description!.trim().isNotEmpty) ...[
                    const Text(
                      'Descripción del Producto',
                      style: TextStyle(
                        fontWeight: FontWeight.bold, 
                        fontSize: 16, 
                        color: Color(0xFF334155),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: const Color(0xFFF8FAFC),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFFE2E8F0)),
                      ),
                      child: Text(
                        product.description!,
                        style: const TextStyle(
                          color: Color(0xFF475569), 
                          fontSize: 14, 
                          height: 1.5,
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],

                  // Presentations List Section (NO PRICES)
                  const Text(
                    'Presentaciones Disponibles',
                    style: TextStyle(
                      fontWeight: FontWeight.bold, 
                      fontSize: 16, 
                      color: Color(0xFF334155),
                    ),
                  ),
                  const SizedBox(height: 12),
                  if (product.presentations.isEmpty)
                    const Text('No hay presentaciones configuradas para este producto.', style: TextStyle(color: Colors.grey))
                  else
                    ListView.builder(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      itemCount: product.presentations.length,
                      itemBuilder: (context, index) {
                        final pres = product.presentations[index];
                        final uomCode = pres.unitOfMeasureCode;
                        final factor = pres.conversionFactor;

                        return Container(
                          margin: const EdgeInsets.only(bottom: 10),
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Row(
                            children: [
                              CircleAvatar(
                                radius: 18,
                                backgroundColor: const Color(0xFFEFF6FF),
                                child: Icon(
                                  pres.isBaseUnit ? Icons.inventory_2_outlined : Icons.layers_outlined,
                                  color: const Color(0xFF2563EB),
                                  size: 18,
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      pres.name,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold, 
                                        fontSize: 14, 
                                        color: Color(0xFF0F172A),
                                      ),
                                    ),
                                    Text(
                                      'Unidad de medida: $uomCode',
                                      style: const TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                                    ),
                                  ],
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFF1F5F9),
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: Text(
                                  factor > 1 
                                      ? 'Contiene ${factor.toInt()} u.' 
                                      : 'Unidad base',
                                  style: const TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF475569),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                  
                  // Bottom Padding so content doesn't get covered by floating button
                  const SizedBox(height: 80),
                ],
              ),
            ),
          ],
        ),
      ),
      // Floating footer for Salesperson actions
      bottomSheet: hasActiveOrder
          ? Container(
              padding: const EdgeInsets.all(20),
              decoration: const BoxDecoration(
                color: Colors.white,
                boxShadow: [
                  BoxShadow(color: Colors.black12, blurRadius: 10, offset: Offset(0, -4)),
                ],
              ),
              child: ElevatedButton.icon(
                icon: const Icon(Icons.add_shopping_cart, color: Colors.white),
                label: const Text(
                  'Agregar al Pedido',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Colors.white),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF0F172A),
                  minimumSize: const Size.fromHeight(50),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                onPressed: () => _showSalespersonAddDialog(context),
              ),
            )
          : null,
    );
  }
}
