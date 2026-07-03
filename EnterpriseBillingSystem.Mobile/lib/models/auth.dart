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

  /// Administrators (SUPER_ADMIN, ADMINISTRADOR) can access all routes
  bool get isAdmin => role == 'SUPER_ADMIN' || role == 'ADMINISTRADOR';

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
