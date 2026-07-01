class ProductPresentation {
  final String id;
  final String productId;
  final String name;
  final String unitOfMeasureId;
  final String unitOfMeasureCode;
  final double conversionFactor;
  final String? barcode;
  final double retailPrice;
  final double semiWholesalePrice;
  final double wholesalePrice;
  final double cost;
  final double taxPercentage;
  final bool isBaseUnit;
  final bool isDefaultSalePresentation;
  final bool isActive;

  ProductPresentation({
    required this.id,
    required this.productId,
    required this.name,
    required this.unitOfMeasureId,
    required this.unitOfMeasureCode,
    required this.conversionFactor,
    this.barcode,
    required this.retailPrice,
    required this.semiWholesalePrice,
    required this.wholesalePrice,
    required this.cost,
    required this.taxPercentage,
    required this.isBaseUnit,
    required this.isDefaultSalePresentation,
    required this.isActive,
  });

  factory ProductPresentation.fromJson(Map<String, dynamic> json) {
    return ProductPresentation(
      id: json['id'] as String? ?? '',
      productId: json['productId'] as String? ?? '',
      name: json['name'] as String? ?? '',
      unitOfMeasureId: json['unitOfMeasureId'] as String? ?? '',
      unitOfMeasureCode: json['unitOfMeasureCode'] as String? ?? '',
      conversionFactor: (json['conversionFactor'] as num?)?.toDouble() ?? 1.0,
      barcode: json['barcode'] as String?,
      retailPrice: (json['retailPrice'] as num?)?.toDouble() ?? 0.0,
      semiWholesalePrice: (json['semiWholesalePrice'] as num?)?.toDouble() ?? (json['semiPrice'] as num?)?.toDouble() ?? 0.0,
      wholesalePrice: (json['wholesalePrice'] as num?)?.toDouble() ?? 0.0,
      cost: (json['cost'] as num?)?.toDouble() ?? 0.0,
      taxPercentage: (json['taxPercentage'] as num?)?.toDouble() ?? 0.0,
      isBaseUnit: json['isBaseUnit'] as bool? ?? false,
      isDefaultSalePresentation: json['isDefaultSalePresentation'] as bool? ?? false,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'productId': productId,
      'name': name,
      'unitOfMeasureId': unitOfMeasureId,
      'unitOfMeasureCode': unitOfMeasureCode,
      'conversionFactor': conversionFactor,
      'barcode': barcode,
      'retailPrice': retailPrice,
      'semiWholesalePrice': semiWholesalePrice,
      'wholesalePrice': wholesalePrice,
      'cost': cost,
      'taxPercentage': taxPercentage,
      'isBaseUnit': isBaseUnit,
      'isDefaultSalePresentation': isDefaultSalePresentation,
      'isActive': isActive,
    };
  }
}

class Product {
  final String id;
  final String internalCode;
  final String? barcode;
  final String name;
  final String? description;
  final String defaultUnitOfMeasureId;
  final String defaultUnitOfMeasureCode;
  final double defaultSalePrice;
  final String? imageUrl;
  final bool isActive;
  final bool isSoldOut;
  final List<ProductPresentation> presentations;
  final String categoryId;
  final String? categoryName;

  Product({
    required this.id,
    required this.internalCode,
    this.barcode,
    required this.name,
    this.description,
    required this.defaultUnitOfMeasureId,
    required this.defaultUnitOfMeasureCode,
    required this.defaultSalePrice,
    this.imageUrl,
    required this.isActive,
    required this.isSoldOut,
    required this.presentations,
    this.categoryId = '',
    this.categoryName,
  });

  factory Product.fromJson(Map<String, dynamic> json) {
    final presentationsJson = json['presentations'] as List<dynamic>?;
    final presentationsList = presentationsJson != null
        ? presentationsJson.map((e) => ProductPresentation.fromJson(e)).toList()
        : <ProductPresentation>[];

    return Product(
      id: json['id'] as String? ?? '',
      internalCode: json['internalCode'] as String? ?? '',
      barcode: json['barcode'] as String?,
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      defaultUnitOfMeasureId: json['defaultUnitOfMeasureId'] as String? ?? '',
      defaultUnitOfMeasureCode: json['defaultUnitOfMeasureCode'] as String? ?? '',
      defaultSalePrice: (json['defaultSalePrice'] as num?)?.toDouble() ?? 0.0,
      imageUrl: json['imageUrl'] as String? ?? json['imagePath'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      isSoldOut: json['isSoldOut'] as bool? ?? false,
      presentations: presentationsList,
      categoryId: json['categoryId'] as String? ?? '',
      categoryName: json['categoryName'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'internalCode': internalCode,
      'barcode': barcode,
      'name': name,
      'description': description,
      'defaultUnitOfMeasureId': defaultUnitOfMeasureId,
      'defaultUnitOfMeasureCode': defaultUnitOfMeasureCode,
      'defaultSalePrice': defaultSalePrice,
      'imageUrl': imageUrl,
      'isActive': isActive,
      'isSoldOut': isSoldOut,
      'presentations': presentations.map((e) => e.toJson()).toList(),
      'categoryId': categoryId,
      'categoryName': categoryName,
    };
  }
}
