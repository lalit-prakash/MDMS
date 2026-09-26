import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class PowerQualityRow {
  final DateTime meterTimeUtc;
  final double voltage;
  final double current;
  final double powerFactor;
  final double frequency;
  final String status;
  final List<String> issues;

  PowerQualityRow.fromJson(Map<String, dynamic> j)
      : meterTimeUtc = DateTime.parse(j['meterTimeUtc'] as String),
        voltage = (j['voltage'] as num).toDouble(),
        current = (j['current'] as num).toDouble(),
        powerFactor = (j['powerFactor'] as num).toDouble(),
        frequency = (j['frequency'] as num).toDouble(),
        status = j['status'] as String,
        issues = (j['issues'] as List).cast<String>();
}

final powerQualityProvider = FutureProvider.autoDispose<PowerQualityRow?>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/power-quality'));
  return response.data == null ? null : PowerQualityRow.fromJson(response.data as Map<String, dynamic>);
});

/// Power Quality dashboard — the latest reading evaluated against tolerance bands defined and
/// computed on the backend (never hardcoded in this screen), per the reference spec's explicit
/// rule. PQ history/trend and harmonics/THD are not implemented (this project only records the
/// latest instantaneous reading per interval, not a rolling PQ-event log).
class PowerQualityScreen extends ConsumerWidget {
  const PowerQualityScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pqAsync = ref.watch(powerQualityProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Power Quality')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(powerQualityProvider),
        child: pqAsync.when(
          data: (pq) {
            if (pq == null) {
              return ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No meter profile data available yet.'))]);
            }
            final isNormal = pq.status == 'NORMAL';
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                HeroGradientCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('STATUS', style: TextStyle(fontSize: 12, letterSpacing: 1, color: Colors.white70)),
                          StatusPill(text: pq.status, color: isNormal ? AppColors.success : AppColors.critical),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(isNormal ? 'Normal' : pq.issues.join(', '), style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 6),
                      Text('as of ${DateFormat.yMMMd().add_jm().format(pq.meterTimeUtc.toLocal())}', style: const TextStyle(fontSize: 12, color: Colors.white70)),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 12,
                  crossAxisSpacing: 12,
                  childAspectRatio: 1.3,
                  children: [
                    StatTile(label: 'Voltage', value: '${pq.voltage.toStringAsFixed(1)} V', icon: Icons.bolt_outlined),
                    StatTile(label: 'Current', value: '${pq.current.toStringAsFixed(2)} A', icon: Icons.flash_on_outlined),
                    StatTile(label: 'Power Factor', value: pq.powerFactor.toStringAsFixed(2), icon: Icons.speed_outlined),
                    StatTile(label: 'Frequency', value: '${pq.frequency.toStringAsFixed(2)} Hz', icon: Icons.graphic_eq_outlined),
                  ],
                ),
              ],
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: Colors.red)))),
        ),
      ),
    );
  }
}
