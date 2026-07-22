import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../services/offline_service.dart';
import '../providers/auth_provider.dart';

class RegisterCustomerScreen extends StatefulWidget {
  final Map<String, dynamic>? editCustomerData;
  const RegisterCustomerScreen({super.key, this.editCustomerData});

  @override
  State<RegisterCustomerScreen> createState() => _RegisterCustomerScreenState();
}

class _RegisterCustomerScreenState extends State<RegisterCustomerScreen> {
  final _formKey = GlobalKey<FormState>();

  // Form Fields
  String _identificationNumber = '';
  int _identificationType = 1; // 1 = Cedula, 2 = RUC, 3 = Passport, 4 = Other
  int _customerType = 1; // 1 = Natural, 2 = LegalEntity
  String _name = '';
  String _legalName = '';
  String? _selectedCategoryId;
  String? _selectedPricingProfileId;
  final double _creditLimit = 0.0;
  final int _creditDays = 0;
  final bool _canUseCredit = false;
  bool _isTaxExempt = false;
  double _defaultDiscountPercentage = 0.0;

  // Contact Info (Directly maps to default Address, Phone, Email lists)
  String _addressLine1 = '';
  String _city = '';
  String _country = 'Nicaragua';
  String _phoneNumber = '';
  String _emailAddress = '';

  // Options lists loaded from backend
  List<dynamic> _categories = [];
  List<dynamic> _pricingProfiles = [];

  bool _isLoading = false;
  bool _isSaving = false;
  String? _errorMessage;

  bool _isEditMode = false;
  String? _addressId;
  String? _phoneId;
  String? _emailId;
  int _status = 1;
  String? _routeId;

  @override
  void initState() {
    super.initState();
    if (widget.editCustomerData != null) {
      final editData = widget.editCustomerData!;
      _isEditMode = true;
      _identificationNumber = editData['identificationNumber'] ?? '';
      _identificationType = editData['identificationType'] ?? 1;
      _customerType = editData['customerType'] ?? 1;
      _name = editData['name'] ?? '';
      _legalName = editData['legalName'] ?? '';
      _selectedCategoryId = editData['customerCategoryId'];
      _selectedPricingProfileId = editData['customerPricingProfileId'];
      _isTaxExempt = editData['isTaxExempt'] ?? false;
      _defaultDiscountPercentage = (editData['defaultDiscountPercentage'] as num?)?.toDouble() ?? 0.0;
      _routeId = editData['routeId'];
      _status = editData['status'] ?? 1;

      // Addresses
      final addresses = editData['addresses'] as List<dynamic>? ?? [];
      if (addresses.isNotEmpty) {
        _addressId = addresses[0]['id'];
        _addressLine1 = addresses[0]['addressLine1'] ?? '';
        _city = addresses[0]['city'] ?? '';
        _country = addresses[0]['country'] ?? 'Nicaragua';
      }

      // Phones
      final phones = editData['phones'] as List<dynamic>? ?? [];
      if (phones.isNotEmpty) {
        _phoneId = phones[0]['id'];
        _phoneNumber = phones[0]['phoneNumber'] ?? '';
      }

      // Emails
      final emails = editData['emails'] as List<dynamic>? ?? [];
      if (emails.isNotEmpty) {
        _emailId = emails[0]['id'];
        _emailAddress = emails[0]['emailAddress'] ?? '';
      }
    }
    _loadConfiguration();
  }

  Future<void> _loadConfiguration() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    final offlineService = OfflineService();
    List<dynamic> loadedCategories = [];
    List<dynamic> loadedProfiles = [];

    try {
      final apiService = Provider.of<ApiService>(context, listen: false);

      // 1. Fetch categories
      final catResponse = await apiService.get('/customercategories?pageNumber=1&pageSize=100');
      if (catResponse.statusCode == 200) {
        final data = jsonDecode(catResponse.body);
        loadedCategories = data['items'] as List<dynamic>? ?? [];
        await offlineService.cacheCustomerCategories(loadedCategories);
      }

      // 2. Fetch pricing profiles
      final profResponse = await apiService.get('/customers/pricing-profiles');
      if (profResponse.statusCode == 200) {
        loadedProfiles = jsonDecode(profResponse.body) as List<dynamic>? ?? [];
        await offlineService.cachePricingProfiles(loadedProfiles);
      }
    } catch (e) {
      // Load from cache on error
      loadedCategories = await offlineService.getCachedCustomerCategories();
      loadedProfiles = await offlineService.getCachedPricingProfiles();

      // Default values if cache is empty
      if (loadedCategories.isEmpty) {
        loadedCategories = [
          {'id': 'cat-default', 'name': 'Categoría General'}
        ];
      }
      if (loadedProfiles.isEmpty) {
        loadedProfiles = [
          {'id': 'prof-default', 'name': 'Lista Detalle', 'type': 1}
        ];
      }
    }

    setState(() {
      _categories = loadedCategories;
      _pricingProfiles = loadedProfiles;
      
      // Select first values if available if not already selected
      if (_selectedCategoryId == null && _categories.isNotEmpty) {
        _selectedCategoryId = _categories[0]['id'];
      }
      if (_selectedPricingProfileId == null && _pricingProfiles.isNotEmpty) {
        final retailProfile = _pricingProfiles.firstWhere(
          (p) => (p['name'] as String? ?? '').toLowerCase().contains('detalle') || p['type'] == 1,
          orElse: () => _pricingProfiles.first,
        );
        _selectedPricingProfileId = retailProfile['id'];
      }
    });

    setState(() {
      _isLoading = false;
    });
  }

  Future<void> _saveCustomer() async {
    if (!_formKey.currentState!.validate()) return;
    _formKey.currentState!.save();

    if (_identificationNumber.trim().isEmpty) {
      _identificationNumber = 'CLI-${DateTime.now().millisecondsSinceEpoch}';
    }

    if (_selectedCategoryId == null || _selectedPricingProfileId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Debe seleccionar una Categoría y un Perfil de Precios.'),
          backgroundColor: Colors.redAccent,
        ),
      );
      return;
    }

    setState(() {
      _isSaving = true;
      _errorMessage = null;
    });

    try {
      final apiService = Provider.of<ApiService>(context, listen: false);

      // Build addresses list
      final List<Map<String, dynamic>> addresses = [];
      if (_addressLine1.trim().isNotEmpty || _city.trim().isNotEmpty) {
        addresses.add({
          'Id': _addressId ?? '00000000-0000-0000-0000-000000000000',
          'AddressLine1': _addressLine1.trim().isNotEmpty ? _addressLine1.trim() : 'Dirección principal',
          'AddressLine2': '',
          'City': _city.trim().isNotEmpty ? _city.trim() : 'Managua',
          'State': '',
          'ZipCode': '',
          'Country': _country.trim().isNotEmpty ? _country.trim() : 'Nicaragua',
          'AddressType': 'Billing',
          'IsDefault': true,
        });
      }

      // Build phones list
      final List<Map<String, dynamic>> phones = [];
      if (_phoneNumber.trim().isNotEmpty) {
        phones.add({
          'Id': _phoneId ?? '00000000-0000-0000-0000-000000000000',
          'PhoneNumber': _phoneNumber.trim(),
          'PhoneType': 'Mobile',
          'IsDefault': true,
        });
      }

      // Build emails list
      final List<Map<String, dynamic>> emails = [];
      if (_emailAddress.trim().isNotEmpty) {
        emails.add({
          'Id': _emailId ?? '00000000-0000-0000-0000-000000000000',
          'EmailAddress': _emailAddress.trim(),
          'EmailType': 'Work',
          'IsDefault': true,
        });
      }

      final body = {
        if (_isEditMode) 'Id': widget.editCustomerData!['id'],
        'IdentificationNumber': _identificationNumber.trim(),
        'IdentificationType': _identificationType,
        'CustomerType': _customerType,
        'Name': _name.trim(),
        'LegalName': _legalName.trim().isNotEmpty ? _legalName.trim() : null,
        'CustomerCategoryId': _selectedCategoryId,
        'CustomerPricingProfileId': _selectedPricingProfileId,
        'CreditLimit': _canUseCredit ? _creditLimit : 0.0,
        'CreditDays': _canUseCredit ? _creditDays : 0,
        'CanUseCredit': _canUseCredit,
        'IsTaxExempt': _isTaxExempt,
        'DefaultDiscountPercentage': _defaultDiscountPercentage,
        if (_isEditMode) 'Status': _status,
        if (_isEditMode) 'RouteId': _routeId,
        'Addresses': addresses,
        'Phones': phones,
        'Emails': emails,
        'Contacts': [],
      };

      final response = _isEditMode
          ? await apiService.put('/customers/${widget.editCustomerData!['id']}', body)
          : await apiService.post('/customers', body);

      if (response.statusCode == 201 || response.statusCode == 200 || response.statusCode == 204) {
        // Success dialog
        if (mounted) {
          showDialog(
            context: context,
            barrierDismissible: false,
            builder: (context) => AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              icon: const Icon(Icons.check_circle_outline_rounded, color: Colors.green, size: 64),
              title: Text(_isEditMode ? 'Cliente Actualizado' : 'Cliente Registrado'),
              content: Text(_isEditMode
                  ? 'El cliente "$_name" ha sido actualizado exitosamente en el sistema.'
                  : 'El cliente "$_name" ha sido creado exitosamente en el sistema.'),
              actions: [
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F172A),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  ),
                  onPressed: () {
                    Navigator.pop(context); // close dialog
                    Navigator.pop(context, true); // return to caller with success flag
                  },
                  child: const Text('Aceptar'),
                )
              ],
            ),
          );
        }
      } else {
        String msg = 'No se pudo crear el cliente.';
        try {
          final errData = jsonDecode(response.body);
          msg = errData['message'] ?? errData['error'] ?? msg;
        } catch (_) {}
        setState(() {
          _errorMessage = msg;
        });
      }
    } catch (e) {
      // Catch connection errors and fallback to local storage
      try {
        final offlineService = OfflineService();
        final tempId = 'temp_${DateTime.now().millisecondsSinceEpoch}';
        
        final List<Map<String, dynamic>> addresses = [];
        if (_addressLine1.trim().isNotEmpty || _city.trim().isNotEmpty) {
          addresses.add({
            'AddressLine1': _addressLine1.trim().isNotEmpty ? _addressLine1.trim() : 'Dirección principal',
            'AddressLine2': '',
            'City': _city.trim().isNotEmpty ? _city.trim() : 'Managua',
            'State': '',
            'ZipCode': '',
            'Country': _country.trim().isNotEmpty ? _country.trim() : 'Nicaragua',
            'AddressType': 'Billing',
            'IsDefault': true,
          });
        }

        final List<Map<String, dynamic>> phones = [];
        if (_phoneNumber.trim().isNotEmpty) {
          phones.add({
            'PhoneNumber': _phoneNumber.trim(),
            'PhoneType': 'Mobile',
            'IsDefault': true,
          });
        }

        final List<Map<String, dynamic>> emails = [];
        if (_emailAddress.trim().isNotEmpty) {
          emails.add({
            'EmailAddress': _emailAddress.trim(),
            'EmailType': 'Work',
            'IsDefault': true,
          });
        }

        final authProv = Provider.of<AuthProvider>(context, listen: false);

        final offlineCustomer = {
          'tempId': tempId,
          'identificationNumber': _identificationNumber.trim(),
          'identificationType': _identificationType,
          'customerType': _customerType,
          'firstName': _name.trim(),
          'lastName': '',
          'legalName': _legalName.trim().isNotEmpty ? _legalName.trim() : null,
          'customerCategoryId': _selectedCategoryId,
          'customerPricingProfileId': _selectedPricingProfileId,
          'creditLimit': _canUseCredit ? _creditLimit : 0.0,
          'creditDays': _canUseCredit ? _creditDays : 0,
          'canUseCredit': _canUseCredit,
          'isTaxExempt': _isTaxExempt,
          'defaultDiscountPercentage': _defaultDiscountPercentage,
          'addresses': addresses,
          'phones': phones,
          'emails': emails,
          'contacts': [],
          'routeId': authProv.userProfile?.routeId,
        };

        await offlineService.saveOfflineCustomer(offlineCustomer);

        if (mounted) {
          showDialog(
            context: context,
            barrierDismissible: false,
            builder: (context) => AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              icon: const Icon(Icons.cloud_off_rounded, color: Colors.orange, size: 64),
              title: const Text('Guardado Localmente'),
              content: Text('Sin conexión a internet. El cliente "$_name" se guardó de forma local en el dispositivo y se sincronizará automáticamente al detectar red.'),
              actions: [
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFE65100),
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  ),
                  onPressed: () {
                    Navigator.pop(context); // close dialog
                    Navigator.pop(context, true); // return to caller with success flag
                  },
                  child: const Text('Aceptar'),
                )
              ],
            ),
          );
        }
      } catch (saveError) {
        setState(() {
          _errorMessage = 'Error de conexión y fallo al guardar localmente: $saveError';
        });
      }
    } finally {
      setState(() {
        _isSaving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9), // Light background
      appBar: AppBar(
        title: const Text('Registrar Cliente'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16.0),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (_errorMessage != null)
                      Container(
                        margin: const EdgeInsets.only(bottom: 16),
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEE2E2),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: const Color(0xFFFCA5A5)),
                        ),
                        child: Text(
                          _errorMessage!,
                          style: const TextStyle(color: Color(0xFF991B1B), fontWeight: FontWeight.w600),
                        ),
                      ),

                    // Section 1: General Info
                    _buildSectionHeader('Información General', Icons.person_rounded),
                    _buildCard([
                      TextFormField(
                        decoration: const InputDecoration(
                          labelText: 'Nombre del Cliente *',
                          hintText: 'Ej. Juan Pérez o Distribuidora ABC',
                          prefixIcon: Icon(Icons.badge_outlined),
                          border: OutlineInputBorder(),
                        ),
                        validator: (value) =>
                            value == null || value.trim().isEmpty ? 'El nombre es requerido.' : null,
                        onSaved: (val) => _name = val ?? '',
                      ),
                    ]),

                    const SizedBox(height: 20),

                    // Section 2: Contact Info
                    _buildSectionHeader('Dirección y Contacto', Icons.contact_mail_rounded),
                    _buildCard([
                      TextFormField(
                        decoration: const InputDecoration(
                          labelText: 'Teléfono Principal',
                          hintText: 'Ej. +505 8888 8888',
                          prefixIcon: Icon(Icons.phone_rounded),
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.phone,
                        onSaved: (val) => _phoneNumber = val ?? '',
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        decoration: const InputDecoration(
                          labelText: 'Correo Electrónico',
                          hintText: 'Ej. juan.perez@ejemplo.com',
                          prefixIcon: Icon(Icons.email_rounded),
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.emailAddress,
                        onSaved: (val) => _emailAddress = val ?? '',
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        decoration: const InputDecoration(
                          labelText: 'Dirección Linea 1',
                          hintText: 'Ej. De la rotonda 2c al lago',
                          prefixIcon: Icon(Icons.location_on_rounded),
                          border: OutlineInputBorder(),
                        ),
                        onSaved: (val) => _addressLine1 = val ?? '',
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: TextFormField(
                              decoration: const InputDecoration(
                                labelText: 'Ciudad',
                                hintText: 'Ej. Managua',
                                border: OutlineInputBorder(),
                              ),
                              onSaved: (val) => _city = val ?? '',
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: TextFormField(
                              initialValue: 'Nicaragua',
                              decoration: const InputDecoration(
                                labelText: 'País',
                                border: OutlineInputBorder(),
                              ),
                              onSaved: (val) => _country = val ?? 'Nicaragua',
                            ),
                          ),
                        ],
                      ),
                    ]),



                    const SizedBox(height: 32),

                    // Submit Button
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF0F172A),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        elevation: 2,
                      ),
                      onPressed: _isSaving ? null : _saveCustomer,
                      child: _isSaving
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                            )
                          : const Text(
                              'GUARDAR CLIENTE',
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, letterSpacing: 1),
                            ),
                    ),
                    const SizedBox(height: 40),
                  ],
                ),
              ),
            ),
    );
  }

  Widget _buildSectionHeader(String title, IconData icon) {
    return Padding(
      padding: const EdgeInsets.only(left: 4, bottom: 8),
      child: Row(
        children: [
          Icon(icon, color: const Color(0xFF64748B), size: 20),
          const SizedBox(width: 8),
          Text(
            title,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: Color(0xFF475569),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCard(List<Widget> children) {
    return Card(
      color: Colors.white,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: Color(0xFFE2E8F0)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: children,
        ),
      ),
    );
  }

  Widget _buildCustomerTypeSelection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tipo de Cliente',
          style: TextStyle(fontWeight: FontWeight.w600, color: Color(0xFF475569), fontSize: 13),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: ChoiceChip(
                label: const Center(child: Text('Persona Natural')),
                selected: _customerType == 1,
                selectedColor: const Color(0xFFE0F2FE),
                checkmarkColor: const Color(0xFF0284C7),
                labelStyle: TextStyle(
                  color: _customerType == 1 ? const Color(0xFF0284C7) : const Color(0xFF64748B),
                  fontWeight: FontWeight.bold,
                ),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                onSelected: (val) {
                  if (val) {
                    setState(() {
                      _customerType = 1;
                    });
                  }
                },
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: ChoiceChip(
                label: const Center(child: Text('Persona Jurídica')),
                selected: _customerType == 2,
                selectedColor: const Color(0xFFE0F2FE),
                checkmarkColor: const Color(0xFF0284C7),
                labelStyle: TextStyle(
                  color: _customerType == 2 ? const Color(0xFF0284C7) : const Color(0xFF64748B),
                  fontWeight: FontWeight.bold,
                ),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                onSelected: (val) {
                  if (val) {
                    setState(() {
                      _customerType = 2;
                    });
                  }
                },
              ),
            ),
          ],
        ),
      ],
    );
  }
}
