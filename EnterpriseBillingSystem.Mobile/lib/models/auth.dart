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
  final int sellerCategory; // 0 = Detail, 1 = Cost

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
    this.sellerCategory = 0,
  });

  String get fullName => '$firstName $lastName'.trim().isNotEmpty 
      ? '$firstName $lastName' 
      : username;

  /// Administrators (SUPER_ADMIN, ADMINISTRADOR) can access all routes
  bool get isAdmin => role == 'SUPER_ADMIN' || role == 'ADMINISTRADOR';

  /// Check if current user is a Cost Seller (Vendedor Costo)
  bool get isCostSeller => sellerCategory == 1;

  /// Returns null for admins so they see all customers from every route
  String? get effectiveRouteId => isAdmin ? null : routeId;

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: json['id'] as String? ?? '',
      username: json['username'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      defaultBranchId: json['defaultBranchId'] as String? ?? '',
      role: json['role'] as String? ?? '',
      permissions: (json['permissions'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ?? 
          [],
      routeId: json['routeId'] as String?,
      sellerCategory: (json['sellerCategory'] as num?)?.toInt() ?? 0,
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
      'sellerCategory': sellerCategory,
    };
  }
}
