class UserProfile {
  final String id;
  final String username;
  final String email;
  final String firstName;
  final String lastName;
  final String defaultBranchId;
  final String role;
  final List<String> permissions;
  final String? routeId;

  UserProfile({
    required this.id,
    required this.username,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.defaultBranchId,
    required this.role,
    required this.permissions,
    this.routeId,
  });

  String get fullName => '$firstName $lastName'.trim().isNotEmpty 
      ? '$firstName $lastName' 
      : username;

  /// Administrators (SUPER_ADMIN, ADMINISTRADOR, ADMIN, GERENTE, etc.)
  bool get isAdmin => role == 'SUPER_ADMIN' || 
                      role == 'ADMINISTRADOR' || 
                      role.toUpperCase().contains('ADMIN') || 
                      role.toUpperCase().contains('GERENTE') ||
                      role.toUpperCase().contains('SUPERVISOR') ||
                      permissions.contains('orders.cost_pricing') ||
                      permissions.contains('products.view_cost');

  /// Returns null for admins so they see all customers from every route
  String? get effectiveRouteId => isAdmin ? null : routeId;

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: json['id']?.toString() ?? '',
      username: json['username']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      firstName: json['firstName']?.toString() ?? '',
      lastName: json['lastName']?.toString() ?? '',
      defaultBranchId: json['defaultBranchId']?.toString() ?? '',
      role: json['role']?.toString() ?? '',
      permissions: (json['permissions'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ?? 
          [],
      routeId: json['routeId']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'username': username,
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'defaultBranchId': defaultBranchId,
      'role': role,
      'permissions': permissions,
      'routeId': routeId,
    };
  }
}
