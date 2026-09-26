import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class MeterOverview {
  final String meterNumber;
  final String phase;
  final String status;
  final DateTime? latestProfileTimeUtc;
  final double? voltage;
  final double? current;
  final double? powerFactor;
  final double? kwh;
  final double? kvah;
  final double? kw;
  final double? maximumDemandKw;
  final DateTime? maximumDemandAtUtc;
  final double? sanctionedLoadKw;
  final String? loadLimitState;

  MeterOverview.fromJson(Map<String, dynamic> j)
      : meterNumber = j['meterNumber'] as String,
        phase = j['phase'] as String,
        status = j['status'] as String,
        latestProfileTimeUtc = j['latestProfileTimeUtc'] != null ? DateTime.parse(j['latestProfileTimeUtc'] as String) : null,
        voltage = (j['voltage'] as num?)?.toDouble(),
        current = (j['current'] as num?)?.toDouble(),
        powerFactor = (j['powerFactor'] as num?)?.toDouble(),
        kwh = (j['kwh'] as num?)?.toDouble(),
        kvah = (j['kvah'] as num?)?.toDouble(),
        kw = (j['kw'] as num?)?.toDouble(),
        maximumDemandKw = (j['maximumDemandKw'] as num?)?.toDouble(),
        maximumDemandAtUtc = j['maximumDemandAtUtc'] != null ? DateTime.parse(j['maximumDemandAtUtc'] as String) : null,
        sanctionedLoadKw = (j['sanctionedLoadKw'] as num?)?.toDouble(),
        loadLimitState = j['loadLimitState'] as String?;
}

final meterOverviewProvider = FutureProvider.autoDispose<MeterOverview?>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  try {
    final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/meter-overview'));
    return MeterOverview.fromJson(response.data as Map<String, dynamic>);
  } on Exception catch (e) {
    if (e.toString().contains('not found') || e.toString().contains('No meter')) return null;
    rethrow;
  }
});

/// Meter Overview + latest 15/30-minute Instantaneous Profile reading + Maximum Demand — the
/// mobile app's "What is my meter capturing?" module (see the reference spec's product
/// principle). Every value here is either the latest available real reading or explicitly absent
/// (never fabricated); "LATEST AVAILABLE" is shown instead of "REAL-TIME" since this reads the
/// last stored profile row, not a live HES push. Power Quality thresholds, a full interval table/
/// chart view, and Meter Events are not implemented in this pass.
class MeterScreen extends ConsumerWidget {
  const MeterScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final overviewAsync = ref.watch(meterOverviewProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Meter')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(meterOverviewProvider),
        child: overviewAsync.when(
          data: (m) {
            if (m == null) {
              return ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No meter is currently assigned to this connection.'))]);
            }
            final hasLatest = m.latestProfileTimeUtc != null;
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
                          Text('Meter No: ${m.meterNumber}', style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600)),
                          StatusPill(text: m.status, color: m.status == 'Installed' ? AppColors.success : AppColors.warning),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Text('${m.phase} Phase', style: const TextStyle(fontSize: 13, color: Colors.white70)),
                      const SizedBox(height: 4),
                      Text(
                        hasLatest ? 'Last Profile: ${DateFormat.yMMMd().add_jm().format(m.latestProfileTimeUtc!.toLocal())}' : 'No profile data received yet',
                        style: const TextStyle(fontSize: 12, color: Colors.white70),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                Text('Latest Available Profile', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                GridView.count(
                  crossAxisCount: 2,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  mainAxisSpacing: 12,
                  crossAxisSpacing: 12,
                  childAspectRatio: 1.3,
                  children: [
                    StatTile(label: 'Voltage', value: hasLatest ? '${m.voltage!.toStringAsFixed(1)} V' : '—', icon: Icons.bolt_outlined),
                    StatTile(label: 'Current', value: hasLatest ? '${m.current!.toStringAsFixed(2)} A' : '—', icon: Icons.flash_on_outlined),
                    StatTile(label: 'Power Factor', value: hasLatest ? m.powerFactor!.toStringAsFixed(2) : '—', icon: Icons.speed_outlined),
                    StatTile(label: 'kWh', value: hasLatest ? m.kwh!.toStringAsFixed(2) : '—', icon: Icons.electric_meter_outlined),
                  ],
                ),
                const SizedBox(height: 16),
                Text('Maximum Demand', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              m.maximumDemandKw != null ? '${m.maximumDemandKw!.toStringAsFixed(2)} kW' : 'No MD recorded',
                              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                            ),
                            if (m.sanctionedLoadKw != null)
                              StatusPill(
                                text: (m.maximumDemandKw ?? 0) > m.sanctionedLoadKw!
                                    ? 'BREACH'
                                    : (m.maximumDemandKw ?? 0) > m.sanctionedLoadKw! * 0.9
                                        ? 'WARNING'
                                        : 'NORMAL',
                                color: (m.maximumDemandKw ?? 0) > m.sanctionedLoadKw!
                                    ? AppColors.critical
                                    : (m.maximumDemandKw ?? 0) > m.sanctionedLoadKw! * 0.9
                                        ? AppColors.warning
                                        : AppColors.success,
                              ),
                          ],
                        ),
                        if (m.sanctionedLoadKw != null) ...[
                          const SizedBox(height: 6),
                          Text('Sanctioned Load: ${m.sanctionedLoadKw!.toStringAsFixed(2)} kW', style: const TextStyle(color: AppColors.cardMuted, fontSize: 12)),
                        ],
                        if (m.maximumDemandAtUtc != null) ...[
                          const SizedBox(height: 2),
                          Text('Recorded ${DateFormat.yMMMd().add_jm().format(m.maximumDemandAtUtc!.toLocal())}', style: const TextStyle(color: AppColors.cardMuted, fontSize: 12)),
                        ],
                      ],
                    ),
                  ),
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
