import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class DailyConsumptionRow {
  final DateTime date;
  final double kwh;
  DailyConsumptionRow.fromJson(Map<String, dynamic> j)
      : date = DateTime.parse(j['date'] as String),
        kwh = (j['consumptionKwh'] as num).toDouble();
}

final dailyConsumptionProvider = FutureProvider.autoDispose<List<DailyConsumptionRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/daily', queryParameters: {'days': 30}));
  return (response.data as List).map((e) => DailyConsumptionRow.fromJson(e as Map<String, dynamic>)).toList();
});

/// Daily Load Profile consumption for the last 30 days — real backend data
/// (CustomersController's consumption/daily endpoint, sourced from DailyLoadProfile). Hourly,
/// weekly/yearly rollups and cost estimation from the full spec are not implemented here; the
/// backend has no tariff-calculation engine yet to source a cost figure from honestly.
class ConsumptionScreen extends ConsumerWidget {
  const ConsumptionScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final consumptionAsync = ref.watch(dailyConsumptionProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Consumption')),
      body: consumptionAsync.when(
        data: (rows) {
          if (rows.isEmpty) {
            return const Center(child: Padding(padding: EdgeInsets.all(24), child: Text('No consumption records available yet.')));
          }
          final today = rows.last.kwh;
          final yesterday = rows.length > 1 ? rows[rows.length - 2].kwh : null;
          final monthTotal = rows.fold<double>(0, (sum, r) => sum + r.kwh);

          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(dailyConsumptionProvider),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Row(
                  children: [
                    Expanded(child: _StatCard(label: "Today", value: '${today.toStringAsFixed(2)} kWh')),
                    const SizedBox(width: 12),
                    Expanded(child: _StatCard(label: 'Yesterday', value: yesterday != null ? '${yesterday.toStringAsFixed(2)} kWh' : '-')),
                  ],
                ),
                const SizedBox(height: 12),
                _StatCard(label: 'Last ${rows.length} days total', value: '${monthTotal.toStringAsFixed(2)} kWh'),
                const SizedBox(height: 24),
                Text('Daily Trend', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                SizedBox(
                  height: 220,
                  child: BarChart(
                    BarChartData(
                      barGroups: [
                        for (var i = 0; i < rows.length; i++)
                          BarChartGroupData(x: i, barRods: [BarChartRodData(toY: rows[i].kwh, color: Colors.teal)])
                      ],
                      titlesData: FlTitlesData(
                        leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: 36)),
                        topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        bottomTitles: AxisTitles(
                          sideTitles: SideTitles(
                            showTitles: true,
                            getTitlesWidget: (value, meta) {
                              final i = value.toInt();
                              if (i < 0 || i >= rows.length || i % 5 != 0) return const SizedBox.shrink();
                              return Padding(
                                padding: const EdgeInsets.only(top: 4),
                                child: Text(DateFormat.Md().format(rows[i].date), style: const TextStyle(fontSize: 10)),
                              );
                            },
                          ),
                        ),
                      ),
                      gridData: const FlGridData(show: true, drawVerticalLine: false),
                      borderData: FlBorderData(show: false),
                    ),
                  ),
                ),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: Colors.red)))),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  const _StatCard({required this.label, required this.value});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 4),
              Text(value, style: Theme.of(context).textTheme.titleLarge),
            ],
          ),
        ),
      );
}
