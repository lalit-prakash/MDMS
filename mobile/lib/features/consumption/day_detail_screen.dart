import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class HourlyPoint {
  final int hour;
  final double kwh;
  HourlyPoint.fromJson(Map<String, dynamic> j)
      : hour = j['hour'] as int,
        kwh = (j['kwh'] as num).toDouble();
}

class IntervalPoint {
  final DateTime intervalStartUtc;
  final DateTime intervalEndUtc;
  final double kwh;
  final double? averageVoltage;
  final double? averageCurrent;
  IntervalPoint.fromJson(Map<String, dynamic> j)
      : intervalStartUtc = DateTime.parse(j['intervalStartUtc'] as String),
        intervalEndUtc = DateTime.parse(j['intervalEndUtc'] as String),
        kwh = (j['kwh'] as num).toDouble(),
        averageVoltage = (j['averageVoltage'] as num?)?.toDouble(),
        averageCurrent = (j['averageCurrent'] as num?)?.toDouble();
}

class DayDetail {
  final DateTime date;
  final double totalKwh;
  final double? peakKw;
  final DateTime? peakAtUtc;
  final double? lowestIntervalKwh;
  final DateTime? lowestAtUtc;
  final double? averageKw;
  final List<HourlyPoint> hourly;
  final List<IntervalPoint> intervals;

  DayDetail.fromJson(Map<String, dynamic> j)
      : date = DateTime.parse(j['date'] as String),
        totalKwh = (j['totalKwh'] as num).toDouble(),
        peakKw = (j['peakKw'] as num?)?.toDouble(),
        peakAtUtc = j['peakAtUtc'] != null ? DateTime.parse(j['peakAtUtc'] as String) : null,
        lowestIntervalKwh = (j['lowestIntervalKwh'] as num?)?.toDouble(),
        lowestAtUtc = j['lowestAtUtc'] != null ? DateTime.parse(j['lowestAtUtc'] as String) : null,
        averageKw = (j['averageKw'] as num?)?.toDouble(),
        hourly = (j['hourly'] as List).map((e) => HourlyPoint.fromJson(e as Map<String, dynamic>)).toList(),
        intervals = (j['intervals'] as List).map((e) => IntervalPoint.fromJson(e as Map<String, dynamic>)).toList();
}

final _dayDetailProvider = FutureProvider.autoDispose.family<DayDetail, DateTime>((ref, date) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/day-detail',
      queryParameters: {'date': DateFormat('yyyy-MM-dd').format(date)}));
  return DayDetail.fromJson(response.data as Map<String, dynamic>);
});

/// Drill-down for a single day, opened by tapping a bar on the Consumption screen's trend chart —
/// hourly totals and the raw 30-minute Load Survey intervals they were built from. "Peak Usage" is
/// each interval's own consumption expressed as an average kW over that half hour, since this
/// project's Load Survey has no true instantaneous kW reading; disclosed as such below the figure.
class DayDetailScreen extends ConsumerStatefulWidget {
  final DateTime initialDate;
  const DayDetailScreen({super.key, required this.initialDate});

  @override
  ConsumerState<DayDetailScreen> createState() => _DayDetailScreenState();
}

class _DayDetailScreenState extends ConsumerState<DayDetailScreen> {
  late DateTime _date;

  @override
  void initState() {
    super.initState();
    _date = DateTime(widget.initialDate.year, widget.initialDate.month, widget.initialDate.day);
  }

  @override
  Widget build(BuildContext context) {
    final detailAsync = ref.watch(_dayDetailProvider(_date));
    final today = DateTime.now();
    final isToday = _date.year == today.year && _date.month == today.month && _date.day == today.day;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            IconButton(icon: const Icon(Icons.chevron_left), onPressed: () => setState(() => _date = _date.subtract(const Duration(days: 1)))),
            Text(DateFormat.yMMMd().format(_date)),
            IconButton(icon: const Icon(Icons.chevron_right), onPressed: isToday ? null : () => setState(() => _date = _date.add(const Duration(days: 1)))),
          ],
        ),
      ),
      body: detailAsync.when(
        data: (d) {
          if (d.intervals.isEmpty) {
            return const Center(child: Padding(padding: EdgeInsets.all(24), child: Text('No interval data available for this day.')));
          }
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Row(
                children: [
                  Expanded(child: StatTile(label: 'Total Consumption', value: '${d.totalKwh.toStringAsFixed(2)} kWh', icon: Icons.bolt)),
                  const SizedBox(width: 12),
                  if (d.peakKw != null)
                    Expanded(
                      child: StatTile(
                        label: 'Peak Usage',
                        value: '${d.peakKw!.toStringAsFixed(2)} kW',
                        subtitle: d.peakAtUtc != null ? 'at ${DateFormat.Hm().format(d.peakAtUtc!.toLocal())}' : null,
                        icon: Icons.trending_up,
                        iconColor: AppColors.warning,
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  if (d.lowestIntervalKwh != null)
                    Expanded(
                      child: StatTile(
                        label: 'Lowest Interval',
                        value: '${d.lowestIntervalKwh!.toStringAsFixed(2)} kWh',
                        subtitle: d.lowestAtUtc != null ? 'at ${DateFormat.Hm().format(d.lowestAtUtc!.toLocal())}' : null,
                        icon: Icons.trending_down,
                        iconColor: AppColors.success,
                      ),
                    ),
                  const SizedBox(width: 12),
                  if (d.averageKw != null)
                    Expanded(child: StatTile(label: 'Average', value: '${d.averageKw!.toStringAsFixed(2)} kW', icon: Icons.show_chart)),
                ],
              ),
              const SizedBox(height: 20),
              Text('Hourly Consumption', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 4),
              const Text('Each interval\'s kWh divided by 0.5h — an average, not an instantaneous meter reading.',
                  style: TextStyle(fontSize: 11, color: AppColors.cardMuted)),
              const SizedBox(height: 12),
              SizedBox(
                height: 200,
                child: BarChart(
                  BarChartData(
                    gridData: const FlGridData(show: true, drawVerticalLine: false),
                    borderData: FlBorderData(show: false),
                    titlesData: FlTitlesData(
                      topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                      rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                      leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: 36)),
                      bottomTitles: AxisTitles(
                        sideTitles: SideTitles(
                          showTitles: true,
                          reservedSize: 24,
                          getTitlesWidget: (value, meta) {
                            final h = value.toInt();
                            if (h < 0 || h > 23 || h % 4 != 0) return const SizedBox.shrink();
                            return Padding(padding: const EdgeInsets.only(top: 4), child: Text('$h:00', style: const TextStyle(fontSize: 9)));
                          },
                        ),
                      ),
                    ),
                    barGroups: [
                      for (final h in d.hourly) BarChartGroupData(x: h.hour, barRods: [BarChartRodData(toY: h.kwh, color: AppColors.accent, width: 8, borderRadius: BorderRadius.circular(3))]),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              Text('Interval Data (30 Minute)', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              Card(
                child: Column(
                  children: d.intervals
                      .map((i) => ListTile(
                            dense: true,
                            title: Text('${DateFormat.Hm().format(i.intervalStartUtc.toLocal())} - ${DateFormat.Hm().format(i.intervalEndUtc.toLocal())}'),
                            subtitle: (i.averageVoltage != null || i.averageCurrent != null)
                                ? Text([
                                    if (i.averageVoltage != null) 'V ${i.averageVoltage!.toStringAsFixed(1)}',
                                    if (i.averageCurrent != null) 'A ${i.averageCurrent!.toStringAsFixed(2)}',
                                  ].join('  ·  '))
                                : null,
                            trailing: Text('${i.kwh.toStringAsFixed(3)} kWh', style: const TextStyle(fontWeight: FontWeight.w600)),
                          ))
                      .toList(),
                ),
              ),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: AppColors.critical)))),
      ),
    );
  }
}
