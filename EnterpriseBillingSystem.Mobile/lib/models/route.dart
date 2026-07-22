class RouteItem {
  final String id;
  final String code;
  final String name;
  final bool isActive;

  RouteItem({
    required this.id,
    required this.code,
    required this.name,
    required this.isActive,
  });

  factory RouteItem.fromJson(Map<String, dynamic> json) {
    return RouteItem(
      id: (json['id'] ?? json['Id']) as String? ?? '',
      code: (json['code'] ?? json['Code']) as String? ?? '',
      name: (json['name'] ?? json['Name']) as String? ?? '',
      isActive: (json['isActive'] ?? json['IsActive']) as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'code': code,
      'name': name,
      'isActive': isActive,
    };
  }
}
