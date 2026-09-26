import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import 'consumption_trend_card.dart';

const _impactColor = Color(0xFF16A34A);

final _monthlyImpactProvider = FutureProvider.autoDispose<EnvironmentalImpact>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/trend', queryParameters: {'range': '30days'}));
  return TrendResponse.fromJson(response.data as Map<String, dynamic>).impact;
});

/// Environmental Impact card: a derived estimate over the last 30 days, built entirely from real
/// inputs (total kWh from Daily Load Profile, total meter power-on hours from Instantaneous
/// Profile) run through two published approximations (CEA grid emission factor, tree-CO2
/// equivalence) computed on the backend -- see EnvironmentalImpactSummary's own doc comment.
/// Never shown as a precise measurement, always as an estimate.
class EnvironmentalImpactCard extends ConsumerWidget {
  const EnvironmentalImpactCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final impactAsync = ref.watch(_monthlyImpactProvider);

    return impactAsync.maybeWhen(
      data: (impact) {
        if (impact.totalKwh <= 0) return const SizedBox.shrink();
        return Card(
          color: _impactColor.withValues(alpha: 0.06),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.eco_outlined, color: _impactColor, size: 20),
                    SizedBox(width: 8),
                    Text('Environmental Impact', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 15)),
                  ],
                ),
                const SizedBox(height: 4),
                const Text('Estimated over the last 30 days', style: TextStyle(fontSize: 11, color: AppColors.cardMuted)),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(child: _ImpactStat(icon: Icons.schedule, value: '${impact.totalPowerOnHours.toStringAsFixed(1)}h', label: 'Meter On Time')),
                    Expanded(child: _ImpactStat(icon: Icons.cloud_outlined, value: impact.co2Kg.toStringAsFixed(1), label: 'kg CO₂ (est.)')),
                    Expanded(child: _ImpactStat(icon: Icons.park_outlined, value: impact.treesSaved.toStringAsFixed(2), label: 'Trees (est.)')),
                  ],
                ),
                const SizedBox(height: 12),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(color: _impactColor.withValues(alpha: 0.10), borderRadius: BorderRadius.circular(12)),
                  child: Text(
                    'Based on ${impact.totalKwh.toStringAsFixed(1)} kWh consumed this month — estimated using standard grid-average CO₂ factors, not a precise measurement.',
                    style: const TextStyle(fontSize: 11, color: _impactColor),
                  ),
                ),
              ],
            ),
          ),
        );
      },
      orElse: () => const SizedBox.shrink(),
    );
  }
}

class _ImpactStat extends StatelessWidget {
  final IconData icon;
  final String value;
  final String label;
  const _ImpactStat({required this.icon, required this.value, required this.label});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, color: _impactColor, size: 20),
        const SizedBox(height: 4),
        Text(value, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: _impactColor)),
        Text(label, style: const TextStyle(fontSize: 10, color: AppColors.cardMuted), textAlign: TextAlign.center),
      ],
    );
  }
}
