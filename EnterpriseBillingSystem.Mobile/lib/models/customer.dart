class Customer {
  final String id;
  final String customerCode;
  final String name;
  final bool isTaxExempt;
  final double defaultDiscountPercentage;
  final int status; // 1 = Active, 2 = Blocked, 3 = Inactive, 4 = Suspended
  final bool canUseCredit;
  final double creditLimit;
  final int creditDays;
  final int customerPricingProfileType; // 0 = Retail, 1 = SemiWholesale, 2 = Wholesale
  final double currentDebt;

  Customer({
    required this.id,
    required this.customerCode,
    required this.name,
    required this.isTaxExempt,
    required this.defaultDiscountPercentage,
    required this.status,
    required this.canUseCredit,
    required this.creditLimit,
    required this.creditDays,
    required this.customerPricingProfileType,
    required this.currentDebt,
  });

  bool get isActive => status == 1;
  bool get isBlocked => status == 2;
  bool get isInactive => status == 3;
  bool get isSuspended => status == 4;

  String get statusText {
    switch (status) {
      case 1:
        return 'Activo';
      case 2:
        return 'Bloqueado';
      case 3:
        return 'Inactivo';
      case 4:
        return 'Suspendido';
      default:
        return 'Desconocido';
    }
  }

  factory Customer.fromJson(Map<String, dynamic> json) {
    // Parse status from JSON. Status might be returned as integer or string
    int statusVal = 1;
    final rawStatus = json['status'];
    if (rawStatus is int) {
      statusVal = rawStatus;
    } else if (rawStatus is String) {
      switch (rawStatus.toLowerCase()) {
        case 'active':
        case '1':
          statusVal = 1;
          break;
        case 'blocked':
        case '2':
          statusVal = 2;
          break;
        case 'inactive':
        case '3':
          statusVal = 3;
          break;
        case 'suspended':
        case '4':
          statusVal = 4;
          break;
        default:
          statusVal = 1;
      }
    }

    return Customer(
      id: json['id'] as String? ?? '',
      customerCode: json['customerCode'] as String? ?? json['code'] as String? ?? '',
      name: json['name'] as String? ?? '',
      isTaxExempt: json['isTaxExempt'] as bool? ?? false,
      defaultDiscountPercentage: (json['defaultDiscountPercentage'] as num?)?.toDouble() ?? 0.0,
      status: statusVal,
      canUseCredit: json['canUseCredit'] as bool? ?? false,
      creditLimit: (json['creditLimit'] as num?)?.toDouble() ?? 0.0,
      creditDays: json['creditDays'] as int? ?? 0,
      customerPricingProfileType: json['customerPricingProfileType'] as int? ?? 0,
      currentDebt: (json['currentDebt'] as num?)?.toDouble() ?? 0.0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'customerCode': customerCode,
      'name': name,
      'isTaxExempt': isTaxExempt,
      'defaultDiscountPercentage': defaultDiscountPercentage,
      'status': status,
      'canUseCredit': canUseCredit,
      'creditLimit': creditLimit,
      'creditDays': creditDays,
      'customerPricingProfileType': customerPricingProfileType,
      'currentDebt': currentDebt,
    };
  }
}
