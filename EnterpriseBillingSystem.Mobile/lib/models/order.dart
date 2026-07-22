class SalesOrderListItem {
  final String id;
  final String orderNumber;
  final String customerId;
  final String customerName;
  final DateTime orderDate;
  final double subTotal;
  final double discountAmount;
  final double taxAmount;
  final double totalAmount;
  final String status;

  SalesOrderListItem({
    required this.id,
    required this.orderNumber,
    required this.customerId,
    required this.customerName,
    required this.orderDate,
    required this.subTotal,
    required this.discountAmount,
    required this.taxAmount,
    required this.totalAmount,
    required this.status,
  });

  factory SalesOrderListItem.fromJson(Map<String, dynamic> json) {
    return SalesOrderListItem(
      id: json['id'] as String? ?? '',
      orderNumber: json['orderNumber'] as String? ?? '',
      customerId: json['customerId'] as String? ?? '',
      customerName: json['customerName'] as String? ?? '',
      orderDate: DateTime.parse(json['orderDate'] as String),
      subTotal: (json['subTotal'] as num?)?.toDouble() ?? 0.0,
      discountAmount: (json['discountAmount'] as num?)?.toDouble() ?? 0.0,
      taxAmount: (json['taxAmount'] as num?)?.toDouble() ?? 0.0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? '',
    );
  }
}

class SalesOrderDetailItem {
  final String id;
  final String productId;
  final String productName;
  final String productCode;
  final String unitOfMeasureId;
  final String unitOfMeasure;
  final double quantity;
  final double unitPrice;
  final double discountPercentage;
  final double discountAmount;
  final double taxPercentage;
  final double taxAmount;
  final double netAmount;

  SalesOrderDetailItem({
    required this.id,
    required this.productId,
    required this.productName,
    required this.productCode,
    required this.unitOfMeasureId,
    required this.unitOfMeasure,
    required this.quantity,
    required this.unitPrice,
    required this.discountPercentage,
    required this.discountAmount,
    required this.taxPercentage,
    required this.taxAmount,
    required this.netAmount,
  });

  factory SalesOrderDetailItem.fromJson(Map<String, dynamic> json) {
    return SalesOrderDetailItem(
      id: json['id'] as String? ?? '',
      productId: json['productId'] as String? ?? '',
      productName: json['productName'] as String? ?? '',
      productCode: json['productCode'] as String? ?? '',
      unitOfMeasureId: json['unitOfMeasureId'] as String? ?? '',
      unitOfMeasure: json['unitOfMeasure'] as String? ?? '',
      quantity: (json['quantity'] as num?)?.toDouble() ?? 0.0,
      unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0.0,
      discountPercentage: (json['discountPercentage'] as num?)?.toDouble() ?? 0.0,
      discountAmount: (json['discountAmount'] as num?)?.toDouble() ?? 0.0,
      taxPercentage: (json['taxPercentage'] as num?)?.toDouble() ?? 0.0,
      taxAmount: (json['taxAmount'] as num?)?.toDouble() ?? 0.0,
      netAmount: (json['netAmount'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class SalesOrderDetail {
  final String id;
  final String orderNumber;
  final String customerId;
  final String customerName;
  final String customerCode;
  final DateTime orderDate;
  final double subTotal;
  final double discountAmount;
  final double taxAmount;
  final double totalAmount;
  final String status;
  final String? notes;
  final DateTime createdOnUtc;
  final List<SalesOrderDetailItem> details;
  final String? createdBy;

  SalesOrderDetail({
    required this.id,
    required this.orderNumber,
    required this.customerId,
    required this.customerName,
    required this.customerCode,
    required this.orderDate,
    required this.subTotal,
    required this.discountAmount,
    required this.taxAmount,
    required this.totalAmount,
    required this.status,
    this.notes,
    required this.createdOnUtc,
    required this.details,
    this.createdBy,
  });

  factory SalesOrderDetail.fromJson(Map<String, dynamic> json) {
    final detailsJson = json['details'] as List<dynamic>?;
    final detailsList = detailsJson != null
        ? detailsJson.map((e) => SalesOrderDetailItem.fromJson(e)).toList()
        : <SalesOrderDetailItem>[];

    return SalesOrderDetail(
      id: json['id'] as String? ?? '',
      orderNumber: json['orderNumber'] as String? ?? '',
      customerId: json['customerId'] as String? ?? '',
      customerName: json['customerName'] as String? ?? '',
      customerCode: json['customerCode'] as String? ?? '',
      orderDate: DateTime.parse(json['orderDate'] as String),
      subTotal: (json['subTotal'] as num?)?.toDouble() ?? 0.0,
      discountAmount: (json['discountAmount'] as num?)?.toDouble() ?? 0.0,
      taxAmount: (json['taxAmount'] as num?)?.toDouble() ?? 0.0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? '',
      notes: json['notes'] as String?,
      createdOnUtc: DateTime.parse(json['createdOnUtc'] as String),
      details: detailsList,
      createdBy: json['createdBy'] as String?,
    );
  }
}

// Helper models for the checkout flow / draft orders in app
class DraftOrderDetail {
  final String productId;
  final String productName;
  final String productCode;
  final String unitOfMeasureId;
  final String unitOfMeasureCode;
  final double quantity;
  final double unitPrice;
  final double discountPercentage;
  final double taxPercentage;

  DraftOrderDetail({
    required this.productId,
    required this.productName,
    required this.productCode,
    required this.unitOfMeasureId,
    required this.unitOfMeasureCode,
    required this.quantity,
    required this.unitPrice,
    this.discountPercentage = 0.0,
    required this.taxPercentage,
  });

  double get subtotal => quantity * unitPrice;
  double get discountAmount => subtotal * (discountPercentage / 100);
  double get baseAmount => subtotal - discountAmount;
  double get taxAmount => baseAmount * (taxPercentage / 100);
  double get netAmount => baseAmount + taxAmount;

  Map<String, dynamic> toJson() {
    return {
      'productId': productId,
      'unitOfMeasureId': unitOfMeasureId,
      'quantity': quantity,
      'unitPrice': unitPrice,
      'discountPercentage': discountPercentage,
      'taxPercentage': taxPercentage,
    };
  }
}

class DraftOrder {
  String? customerId;
  String? customerName;
  bool isCustomerTaxExempt;
  double customerDefaultDiscount;
  DateTime orderDate;
  String? notes;
  List<DraftOrderDetail> details;

  DraftOrder({
    this.customerId,
    this.customerName,
    this.isCustomerTaxExempt = false,
    this.customerDefaultDiscount = 0.0,
    required this.orderDate,
    this.notes,
    required this.details,
  });

  double get subTotal => details.fold(0.0, (sum, item) => sum + item.subtotal);
  double get discountAmount => details.fold(0.0, (sum, item) => sum + item.discountAmount);
  double get taxAmount {
    if (isCustomerTaxExempt) return 0.0;
    return details.fold(0.0, (sum, item) => sum + item.taxAmount);
  }
  double get totalAmount => subTotal - discountAmount + taxAmount;

  Map<String, dynamic> toJson() {
    return {
      'customerId': customerId,
      'orderDate': orderDate.toIso8601String(),
      'notes': notes,
      'details': details.map((e) => e.toJson()).toList(),
    };
  }
}
