import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';

class GoalsScreen extends StatefulWidget {
  const GoalsScreen({super.key});

  @override
  State<GoalsScreen> createState() => _GoalsScreenState();
}

class _GoalsScreenState extends State<GoalsScreen> {
  List<dynamic> _goals = [];
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _fetchGoals();
  }

  Future<void> _fetchGoals() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final apiService = Provider.of<ApiService>(context, listen: false);
      final response = await apiService.get('/sales-goals/my-goals');

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        setState(() {
          _goals = data;
        });
      } else {
        setState(() {
          _errorMessage = 'Error al cargar metas de ventas (${response.statusCode})';
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'Error de conexión: $e';
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  String _formatCurrency(double amount) {
    // Standard format for NIO (Córdobas) or USD
    return 'C\$ ${amount.toStringAsFixed(2).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        )}';
  }

  String _formatDate(String dateStr) {
    try {
      final dt = DateTime.parse(dateStr);
      return '${dt.day.toString().padLeft(2, '0')}/${dt.month.toString().padLeft(2, '0')}/${dt.year}';
    } catch (_) {
      return dateStr;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF1F5F9), // Light background
      appBar: AppBar(
        title: const Text('Mis Metas de Venta'),
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Actualizar',
            onPressed: _fetchGoals,
          )
        ],
      ),
      body: _isLoading && _goals.isEmpty
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _fetchGoals,
              color: const Color(0xFF0F172A),
              child: _errorMessage != null && _goals.isEmpty
                  ? _buildErrorState()
                  : _goals.isEmpty
                      ? _buildEmptyState()
                      : _buildGoalsList(),
            ),
    );
  }

  Widget _buildErrorState() {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        SizedBox(height: MediaQuery.of(context).size.height * 0.25),
        Center(
          child: Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              children: [
                const Icon(Icons.error_outline_rounded, size: 64, color: Colors.redAccent),
                const SizedBox(height: 16),
                const Text(
                  'Ocurrió un inconveniente',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
                ),
                const SizedBox(height: 8),
                Text(
                  _errorMessage ?? 'No se pudo establecer conexión con el servidor.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Color(0xFF64748B)),
                ),
                const SizedBox(height: 24),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF0F172A),
                    foregroundColor: Colors.white,
                  ),
                  onPressed: _fetchGoals,
                  child: const Text('Reintentar'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildEmptyState() {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        SizedBox(height: MediaQuery.of(context).size.height * 0.25),
        const Center(
          child: Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              children: [
                Icon(Icons.emoji_events_outlined, size: 64, color: Color(0xFF94A3B8)),
                const SizedBox(height: 16),
                Text(
                  'Sin metas asignadas',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF1E293B)),
                ),
                const SizedBox(height: 8),
                Text(
                  'El administrador aún no te ha asignado metas de ventas para este periodo.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xFF64748B)),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildGoalsList() {
    // Sort goals so active ones appear first
    final activeGoals = _goals.where((g) => g['isActive'] == true).toList();
    final inactiveGoals = _goals.where((g) => g['isActive'] == false).toList();
    final sortedGoals = [...activeGoals, ...inactiveGoals];

    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16.0),
      itemCount: sortedGoals.length,
      itemBuilder: (context, index) {
        final goal = sortedGoals[index];
        final bool isActive = goal['isActive'] ?? false;
        final double target = (goal['targetAmount'] as num).toDouble();
        final double current = (goal['currentAmount'] as num).toDouble();
        final double progress = (goal['progressPercentage'] as num).toDouble();
        final double remaining = (goal['remainingAmount'] as num).toDouble();
        final String period = goal['periodName'] ?? 'General';
        
        final double displayProgress = (progress / 100).clamp(0.0, 1.0);
        final bool isCompleted = progress >= 100.0;

        return Card(
          margin: const EdgeInsets.only(bottom: 16.0),
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(
              color: isActive 
                  ? (isCompleted ? const Color(0xFF10B981) : const Color(0xFFE2E8F0))
                  : const Color(0xFFCBD5E1),
              width: isActive && isCompleted ? 2.0 : 1.0,
            ),
          ),
          child: Padding(
            padding: const EdgeInsets.all(18.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top header: Period & Status Badge
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      period,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF0F172A),
                      ),
                    ),
                    _buildStatusBadge(isActive, isCompleted),
                  ],
                ),
                const SizedBox(height: 4),
                // Date range description
                Text(
                  'Válido del ${_formatDate(goal['startDate'])} al ${_formatDate(goal['endDate'])}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: Color(0xFF64748B),
                  ),
                ),
                const Divider(height: 24),

                // Main Stats row
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Vendido actual',
                            style: TextStyle(fontSize: 11, color: Color(0xFF64748B)),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            _formatCurrency(current),
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: isCompleted ? const Color(0xFF10B981) : const Color(0xFF0F172A),
                            ),
                          ),
                        ],
                      ),
                    ),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Monto Meta',
                            style: TextStyle(fontSize: 11, color: Color(0xFF64748B)),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            _formatCurrency(target),
                            style: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Color(0xFF0F172A),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Progress Bar with Percentage Label
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Progreso',
                      style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: Color(0xFF475569)),
                    ),
                    Text(
                      '${progress.toStringAsFixed(1)}%',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: isCompleted ? const Color(0xFF10B981) : const Color(0xFF0F172A),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: LinearProgressIndicator(
                    value: displayProgress,
                    minHeight: 10,
                    backgroundColor: const Color(0xFFE2E8F0),
                    color: isCompleted 
                        ? const Color(0xFF10B981) 
                        : (isActive ? const Color(0xFF38BDF8) : const Color(0xFF94A3B8)),
                  ),
                ),
                const SizedBox(height: 12),

                // Bottom notes / remaining indicator
                if (isActive)
                  Row(
                    children: [
                      Icon(
                        isCompleted ? Icons.check_circle_rounded : Icons.info_outline_rounded,
                        size: 14,
                        color: isCompleted ? const Color(0xFF10B981) : const Color(0xFF38BDF8),
                      ),
                      const SizedBox(width: 6),
                      Text(
                        isCompleted
                            ? '¡Felicidades! Completaste la meta de ventas.'
                            : 'Faltan ${_formatCurrency(remaining)} para lograr la meta.',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w500,
                          color: isCompleted ? const Color(0xFF10B981) : const Color(0xFF475569),
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildStatusBadge(bool isActive, bool isCompleted) {
    Color bg;
    Color fg;
    String text;

    if (!isActive) {
      bg = const Color(0xFFE2E8F0);
      fg = const Color(0xFF64748B);
      text = 'Inactiva';
    } else if (isCompleted) {
      bg = const Color(0xFFD1FAE5);
      fg = const Color(0xFF065F46);
      text = 'Lograda';
    } else {
      bg = const Color(0xFFE0F2FE);
      fg = const Color(0xFF0369A1);
      text = 'En Progreso';
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.bold,
          color: fg,
        ),
      ),
    );
  }
}
