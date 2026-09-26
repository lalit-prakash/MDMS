import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../dashboard/dashboard_screen.dart';

class IntervalProfileRow {
  final DateTime meterTimeUtc;
  final double voltage;
  final double current;
  final double powerFactor;
  final double kwh;
  final double kvah;

  IntervalProfileRow.fromJson(Map<String, dynamic> j)
      : meterTimeUtc = DateTime.parse(j['meterTimeUtc'] as String),
        voltage = (j['voltage'] as num).toDouble(),
        current = (j['phaseCurrent'] as num).toDouble(),
        powerFactor = (j['powerFactor'] as num).toDouble(),
        kwh = (j['kwh'] as num).toDouble(),
        kvah = (j['kvah'] as num).toDouble();
}

enum _RangeOption { today, yesterday, last7Days }

final _intervalProfileProvider = FutureProvider.autoDispose.family<List<IntervalProfileRow>, _RangeOption>((ref, range) async {
  final client = ref.watch(apiClientProvider);
  final summary = await ref.watch(summaryProvider.future);
  // The consumer's own meter-scoped Instantaneous Profile — reuses the same
  // /api/v1/meter-data/ip endpoint the utility console's Meter Data screen uses, filtered to
  // this consumer's meterId so no separate consumer-facing endpoint duplicates the same query.
  if (summary.meterId == null) return [];
  final now = DateTime.now().toUtc();
  late DateTime from;
  late DateTime to;
  switch (range) {
    case _RangeOption.today:
      from = DateTime.utc(now.year, now.month, now.day);
      to = from.add(const Duration(days: 1));
    case _RangeOption.yesterday:
      from = DateTime.utc(now.year, now.month, now.day).subtract(const Duration(days: 1));
      to = from.add(const Duration(days: 1));
    case _RangeOption.last7Days:
      from = now.subtract(const Duration(days: 7));
      to = now;
  }

  final response = await apiCall(() => client.dio.get('/api/v1/meter-data/ip', queryParameters: {
        'meterId': summary.meterId,
        'fromDate': from.toIso8601String(),
        'toDate': to.toIso8601String(),
        'pageSize': 100,
      }));
  final data = (response.data as Map<String, dynamic>)['data'] as List;
  return data.map((e) => IntervalProfileRow.fromJson(e as Map<String, dynamic>)).toList().reversed.toList();
});

/// 15/30-minute Interval Meter Profile — table view of real Instantaneous Profile readings
/// (never fabricated; a gap in the table simply means no reading was captured for that slot,
/// which the reference spec calls "Missing" — this screen doesn't yet render an explicit
/// per-row availability status, just the rows that exist). Chart view and phase-wise breakdown
/// for three-phase meters are not implemented in this pass.
class IntervalProfileScreen extends ConsumerStatefulWidget {
  const IntervalProfileScreen({super.key});

  @override
  ConsumerState<IntervalProfileScreen> createState() => _IntervalProfileScreenState();
}

class _IntervalProfileScreenState extends ConsumerState<IntervalProfileScreen> {
  _RangeOption _range = _RangeOption.today;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Meter Interval Profile')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: SegmentedButton<_RangeOption>(
              segments: const [
                ButtonSegment(value: _RangeOption.today, label: Text('Today')),
                ButtonSegment(value: _RangeOption.yesterday, label: Text('Yesterday')),
                ButtonSegment(value: _RangeOption.last7Days, label: Text('7 Days')),
              ],
              selected: {_range},
              onSelectionChanged: (s) => setState(() => _range = s.first),
            ),
          ),
          Expanded(
            child: Consumer(
              builder: (context, ref, _) {
                final rowsAsync = ref.watch(_intervalProfileProvider(_range));
                return rowsAsync.when(
                  data: (rows) => rows.isEmpty
                      ? const Center(child: Text('No profile data available for this period.'))
                      : ListView.builder(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          itemCount: rows.length,
                          itemBuilder: (context, i) {
                            final r = rows[i];
                            return Card(
                              child: ListTile(
                                dense: true,
                                title: Text(DateFormat.Hm().format(r.meterTimeUtc.toLocal())),
                                subtitle: Text('V ${r.voltage.toStringAsFixed(1)}  ·  A ${r.current.toStringAsFixed(2)}  ·  PF ${r.powerFactor.toStringAsFixed(2)}'),
                                trailing: Text('${r.kwh.toStringAsFixed(2)} kWh', style: const TextStyle(fontWeight: FontWeight.w600)),
                              ),
                            );
                          },
                        ),
                  loading: () => const Center(child: CircularProgressIndicator()),
                  error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: AppColors.critical)))),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
