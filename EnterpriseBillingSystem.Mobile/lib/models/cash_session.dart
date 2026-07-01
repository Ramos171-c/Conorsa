class CashSession {
  final String id;
  final String cashRegisterId;
  final String cashRegisterName;
  final String openedByUserName;
  final DateTime openedAt;
  final DateTime? closedAt;
  final double openingCash;
  final double currentBalance;
  final String status; // 'Open' o 'Closed'

  CashSession({
    required this.id,
    required this.cashRegisterId,
    required this.cashRegisterName,
    required this.openedByUserName,
    required this.openedAt,
    this.closedAt,
    required this.openingCash,
    required this.currentBalance,
    required this.status,
  });

  bool get isOpen => status.toLowerCase() == 'open' && closedAt == null;

  factory CashSession.fromJson(Map<String, dynamic> json) {
    return CashSession(
      id: json['id'] as String? ?? '',
      cashRegisterId: json['cashRegisterId'] as String? ?? '',
      cashRegisterName: json['cashRegisterName'] as String? ?? '',
      openedByUserName: json['openedByUserName'] as String? ?? '',
      openedAt: json['openedAt'] != null ? DateTime.parse(json['openedAt']) : DateTime.now(),
      closedAt: json['closedAt'] != null ? DateTime.parse(json['closedAt']) : null,
      openingCash: (json['openingCash'] as num?)?.toDouble() ?? 0.0,
      currentBalance: (json['currentBalance'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? 'Closed',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'cashRegisterId': cashRegisterId,
      'cashRegisterName': cashRegisterName,
      'openedByUserName': openedByUserName,
      'openedAt': openedAt.toIso8601String(),
      'closedAt': closedAt?.toIso8601String(),
      'openingCash': openingCash,
      'currentBalance': currentBalance,
      'status': status,
    };
  }
}
