import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/order_provider.dart';
import '../widgets/cached_product_image.dart';
import 'login_screen.dart';
import 'create_order_screen.dart';
import 'product_detail_screen.dart';

class CatalogScreen extends StatefulWidget {
  const CatalogScreen({super.key});

  @override
  State<CatalogScreen> createState() => _CatalogScreenState();
}

class _CatalogScreenState extends State<CatalogScreen> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<OrderProvider>(context, listen: false).fetchProducts();
    });
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    final search = _searchController.text.trim();
    Provider.of<OrderProvider>(context, listen: false).fetchProducts(search: search);
  }

  @override
  Widget build(BuildContext context) {
    final provider = Provider.of<OrderProvider>(context);
    final auth = Provider.of<AuthProvider>(context);
    final width = MediaQuery.of(context).size.width;

    // Responsive Column count: 3 on tablets (width > 600), 2 on phones
    final columns = width > 600 ? 3 : 2;

    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text('Catálogo de Productos'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          if (auth.isLoggedIn) ...[
            // Cart shortcut button for quick checkout access when seller logged in
            IconButton(
              icon: Badge(
                label: Text(provider.draftOrder.details.length.toString()),
                child: const Icon(Icons.shopping_cart_rounded),
              ),
              tooltip: 'Ver Pedido Actual',
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const CreateOrderScreen()),
                );
              },
            ),
            IconButton(
              icon: const Icon(Icons.dashboard_rounded),
              tooltip: 'Volver a Panel',
              onPressed: () => Navigator.pop(context),
            ),
          ] else ...[
            TextButton.icon(
              icon: const Icon(Icons.lock_open_rounded, color: Color(0xFF38BDF8), size: 20),
              label: const Text(
                'Acceso Vendedor',
                style: TextStyle(color: Color(0xFF38BDF8), fontWeight: FontWeight.bold),
              ),
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const LoginScreen()),
                );
              },
            ),
          ],
        ],
      ),
      body: Column(
        children: [
          // Public Search Banner
          Container(
            color: const Color(0xFF0F172A),
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 20, top: 4),
            child: TextField(
              controller: _searchController,
              style: const TextStyle(color: Colors.white),
              decoration: InputDecoration(
                prefixIcon: const Icon(Icons.search, color: Colors.white60),
                hintText: 'Buscar productos en el menú...',
                hintStyle: const TextStyle(color: Colors.white38),
                filled: true,
                fillColor: const Color(0xFF1E293B),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide.none,
                ),
                contentPadding: const EdgeInsets.symmetric(vertical: 12),
              ),
            ),
          ),

          // Responsive Grid View
          Expanded(
            child: provider.isLoading && provider.products.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : provider.products.isEmpty
                    ? const Center(child: Text('No hay productos disponibles.'))
                    : RefreshIndicator(
                        onRefresh: () => provider.fetchProducts(),
                        child: GridView.builder(
                          padding: const EdgeInsets.all(16),
                          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: columns,
                            crossAxisSpacing: 16,
                            mainAxisSpacing: 16,
                            childAspectRatio: 0.72, // Tighter ratio for cards
                          ),
                          itemCount: provider.products.length,
                          itemBuilder: (context, index) {
                            final product = provider.products[index];
                            final isSoldOut = product.isSoldOut;

                            return Card(
                              elevation: isSoldOut ? 0 : 2,
                              color: isSoldOut ? const Color(0xFFF1F5F9) : Colors.white,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(16),
                                side: BorderSide(
                                  color: isSoldOut ? const Color(0xFFE2E8F0) : Colors.transparent,
                                ),
                              ),
                              child: InkWell(
                                onTap: () {
                                  Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                      builder: (context) => ProductDetailScreen(product: product),
                                    ),
                                  );
                                },
                                borderRadius: BorderRadius.circular(16),
                                child: Stack(
                                  children: [
                                    // Main content
                                    Opacity(
                                      opacity: isSoldOut ? 0.45 : 1.0,
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.stretch,
                                        children: [
                                          // Product Image
                                          Expanded(
                                            child: ClipRRect(
                                              borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
                                              child: GestureDetector(
                                                onTap: () => _showFullImageDialog(context, product.imageUrl, product.name),
                                                child: CachedProductImage(
                                                  imageUrl: product.imageUrl,
                                                  width: double.infinity,
                                                  fit: BoxFit.cover,
                                                  iconSize: 50,
                                                ),
                                              ),
                                            ),
                                          ),
                                          Padding(
                                            padding: const EdgeInsets.all(12.0),
                                            child: Column(
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                Text(
                                                  product.name,
                                                  maxLines: 2,
                                                  overflow: TextOverflow.ellipsis,
                                                  style: const TextStyle(
                                                    fontWeight: FontWeight.bold,
                                                    fontSize: 14,
                                                    color: Color(0xFF0F172A),
                                                  ),
                                                ),
                                                const SizedBox(height: 4),
                                                Text(
                                                  'Código: ${product.internalCode}',
                                                  style: const TextStyle(
                                                    color: Color(0xFF64748B), 
                                                    fontSize: 11,
                                                  ),
                                                ),
                                                const SizedBox(height: 8),
                                                Text(
                                                  '\$${product.defaultSalePrice.toStringAsFixed(2)}',
                                                  style: TextStyle(
                                                    fontWeight: FontWeight.bold,
                                                    fontSize: 16,
                                                    color: isSoldOut ? const Color(0xFF64748B) : const Color(0xFF1E3A8A),
                                                  ),
                                                ),
                                              ],
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),

                                    // Sold Out Red Ribbon Overlay
                                    if (isSoldOut)
                                      Positioned(
                                        top: 32,
                                        left: -20,
                                        right: -20,
                                        child: Transform.rotate(
                                          angle: -0.2, // Slight tilt
                                          child: Container(
                                            color: Colors.red.shade600,
                                            padding: const EdgeInsets.symmetric(vertical: 6),
                                            child: const Text(
                                              'AGOTADO',
                                              textAlign: TextAlign.center,
                                              style: TextStyle(
                                                color: Colors.white,
                                                fontWeight: FontWeight.bold,
                                                fontSize: 12,
                                                letterSpacing: 1.5,
                                              ),
                                            ),
                                          ),
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

  void _showFullImageDialog(BuildContext context, String? imageUrl, String productName) {
    showDialog(
      context: context,
      barrierDismissible: true,
      builder: (context) {
        return Dialog(
          backgroundColor: Colors.transparent,
          insetPadding: const EdgeInsets.all(16),
          child: Stack(
            alignment: Alignment.center,
            children: [
              Container(
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(16),
                ),
                padding: const EdgeInsets.all(8),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(12),
                      child: CachedProductImage(
                        imageUrl: imageUrl,
                        width: double.infinity,
                        height: 350,
                        fit: BoxFit.contain,
                        iconSize: 100,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Text(
                      productName,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF0F172A),
                      ),
                    ),
                    const SizedBox(height: 4),
                  ],
                ),
              ),
              Positioned(
                top: 8,
                right: 8,
                child: IconButton(
                  icon: const Icon(Icons.close, color: Colors.grey),
                  onPressed: () => Navigator.pop(context),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
