import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import 'day_detail_screen.dart';

class DailyConsumptionRow {
  final DateTime date;
  final double kwh;
  DailyConsumptionRow.fromJson(Map<String, dynamic> j)
      : date = DateTime.parse(j['date'] as String),
        kwh = (j['consumptionKwh'] as num).toDouble();
}

final dailyConsumptionProvider = FutureProvider.autoDispose.family<List<DailyConsumptionRow>, int>((ref, days) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/daily', queryParameters: {'days': days}));
  return (response.data as List).map((e) => DailyConsumptionRow.fromJson(e as Map<String, dynamic>)).toList();
});

class MonthlyComparison {
  final String currentMonthLabel;
  final double currentMonthKwh;
  final String? previousMonthLabel;
  final double? previousMonthKwh;
  final int? peakUsageHour;
  final double? peakUsageHourAvgKwh;
  MonthlyComparison.fromJson(Map<String, dynamic> j)
      : currentMonthLabel = j['currentMonthLabel'] as String,
        currentMonthKwh = (j['currentMonthKwh'] as num).toDouble(),
        previousMonthLabel = j['previousMonthLabel'] as String?,
        previousMonthKwh = (j['previousMonthKwh'] as num?)?.toDouble(),
        peakUsageHour = j['peakUsageHour'] as int?,
        peakUsageHourAvgKwh = (j['peakUsageHourAvgKwh'] as num?)?.toDouble();
}

final _monthlyComparisonProvider = FutureProvider.autoDispose<MonthlyComparison>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/monthly-comparison'));
  return MonthlyComparison.fromJson(response.data as Map<String, dynamic>);
});

enum _TrendRange { days7, days30 }

/// Consumption module — Today/Yesterday/Last-N-days totals, a tappable Daily Consumption Trend
/// (tap a bar to drill into that day's hourly breakdown via DayDetailScreen), Peak Usage,
/// Monthly Comparison and a real Energy Insight (the hour-of-day this meter draws the most power
/// on average, over the last 30 days — never a canned tip). All real Daily Load Profile /
/// Load Survey data; 90-day/custom ranges and an hourly/monthly top-tab switcher from the
/// reference design are not implemented — this project's seeded history and this screen don't
/// yet support them honestly.
class ConsumptionScreen extends ConsumerStatefulWidget {
  const ConsumptionScreen({super.key});

  @override
  ConsumerState<ConsumptionScreen> createState() => _ConsumptionScreenState();
}

class _ConsumptionScreenState extends ConsumerState<ConsumptionScreen> {
  _TrendRange _range = _TrendRange.days30;

  @override
  Widget build(BuildContext context) {
    final days = _range == _TrendRange.days7 ? 7 : 30;
    final consumptionAsync = ref.watch(dailyConsumptionProvider(days));
    final monthlyAsync = ref.watch(_monthlyComparisonProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Consumption')),
      body: consumptionAsync.when(
        data: (rows) {
          if (rows.isEmpty) {
            return const Center(child: Padding(padding: EdgeInsets.all(24), child: Text('No consumption records available yet.')));
          }
          final today = rows.last.kwh;
          final yesterday = rows.length > 1 ? rows[rows.length - 2].kwh : null;
          final periodTotal = rows.fold<double>(0, (sum, r) => sum + r.kwh);
          final peak = rows.reduce((a, b) => a.kwh >= b.kwh ? a : b);
          final lowest = rows.reduce((a, b) => a.kwh <= b.kwh ? a : b);

          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(dailyConsumptionProvider(days));
              ref.invalidate(_monthlyComparisonProvider);
            },
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Row(
                  children: [
                    Expanded(child: _StatCard(label: 'Today', value: '${today.toStringAsFixed(2)} kWh')),
                    const SizedBox(width: 12),
                    Expanded(child: _StatCard(label: 'Yesterday', value: yesterday != null ? '${yesterday.toStringAsFixed(2)} kWh' : '-')),
                  ],
                ),
                const SizedBox(height: 12),
                _StatCard(label: 'Last ${rows.length} Days', value: '${periodTotal.toStringAsFixed(2)} kWh', subtitle: 'Avg ${(periodTotal / rows.length).toStringAsFixed(2)} kWh/day'),
                const SizedBox(height: 24),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Daily Consumption Trend', style: Theme.of(context).textTheme.titleMedium),
                    _RangeToggle(value: _range, onChanged: (v) => setState(() => _range = v)),
                  ],
                ),
                const SizedBox(height: 4),
                const Text('Tap a bar to see that day\'s hourly breakdown.', style: TextStyle(fontSize: 11, color: AppColors.cardMuted)),
                const SizedBox(height: 12),
                SizedBox(
                  height: 220,
                  child: BarChart(
                    BarChartData(
                      barTouchData: BarTouchData(
                        touchTooltipData: BarTouchTooltipData(getTooltipColor: (_) => AppColors.accent),
                        touchCallback: (event, response) {
                          if (!event.isInterestedForInteractions) return;
                          final index = response?.spot?.touchedBarGroupIndex;
                          if (index == null || index < 0 || index >= rows.length) return;
                          Navigator.of(context).push(MaterialPageRoute(builder: (_) => DayDetailScreen(initialDate: rows[index].date)));
                        },
                      ),
                      barGroups: [
                        for (var i = 0; i < rows.length; i++)
                          BarChartGroupData(x: i, barRods: [BarChartRodData(toY: rows[i].kwh, color: AppColors.accent, width: days == 30 ? 5 : 14, borderRadius: BorderRadius.circular(3))])
                      ],
                      titlesData: FlTitlesData(
                        leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: 36)),
                        topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                        bottomTitles: AxisTitles(
                          sideTitles: SideTitles(
                            showTitles: true,
                            reservedSize: 24,
                            getTitlesWidget: (value, meta) {
                              final i = value.toInt();
                              final step = (rows.length / 6).ceil().clamp(1, rows.length);
                              if (i < 0 || i >= rows.length || i % step != 0) return const SizedBox.shrink();
                              return Padding(padding: const EdgeInsets.only(top: 4), child: Text(DateFormat.Md().format(rows[i].date), style: const TextStyle(fontSize: 10)));
                            },
                          ),
                        ),
                      ),
                      gridData: const FlGridData(show: true, drawVerticalLine: false),
                      borderData: FlBorderData(show: false),
                    ),
                  ),
                ),
                const SizedBox(height: 20),
                Row(
                  children: [
                    Expanded(
                      child: InkWell(
                        onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => DayDetailScreen(initialDate: peak.date))),
                        child: StatTile(label: 'Highest Day', value: '${peak.kwh.toStringAsFixed(2)} kWh', subtitle: DateFormat.yMMMd().format(peak.date), icon: Icons.trending_up, iconColor: AppColors.warning),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: InkWell(
                        onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => DayDetailScreen(initialDate: lowest.date))),
                        child: StatTile(label: 'Lowest Day', value: '${lowest.kwh.toStringAsFixed(2)} kWh', subtitle: DateFormat.yMMMd().format(lowest.date), icon: Icons.trending_down, iconColor: AppColors.success),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                monthlyAsync.when(
                  data: (m) => Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Monthly Comparison', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(m.currentMonthLabel, style: const TextStyle(fontSize: 12, color: AppColors.cardMuted)),
                                    Text('${m.currentMonthKwh.toStringAsFixed(2)} kWh', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                                  ],
                                ),
                              ),
                              if (m.previousMonthKwh != null) ...[
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(m.previousMonthLabel ?? '', style: const TextStyle(fontSize: 12, color: AppColors.cardMuted)),
                                      Text('${m.previousMonthKwh!.toStringAsFixed(2)} kWh', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                                    ],
                                  ),
                                ),
                                _ChangePill(current: m.currentMonthKwh, previous: m.previousMonthKwh!),
                              ],
                            ],
                          ),
                        ),
                      ),
                      if (m.peakUsageHour != null) ...[
                        const SizedBox(height: 16),
                        Text('Energy Insight', style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 8),
                        Card(
                          color: AppColors.accent.withValues(alpha: 0.06),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Row(
                              children: [
                                const Icon(Icons.lightbulb_outline, color: AppColors.accent),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Text(
                                    'Over the last 30 days, your electricity usage is highest around ${_hourLabel(m.peakUsageHour!)} '
                                    '(avg ${m.peakUsageHourAvgKwh!.toStringAsFixed(2)} kWh/interval). Shifting high-load appliances away from this window can help reduce cost.',
                                    style: const TextStyle(fontSize: 13),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                  loading: () => const SizedBox.shrink(),
                  error: (e, _) => const SizedBox.shrink(),
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

  String _hourLabel(int hour) {
    final start = TimeOfDay(hour: hour, minute: 0).format(context);
    final end = TimeOfDay(hour: (hour + 1) % 24, minute: 0).format(context);
    return '$start - $end';
  }
}

class _RangeToggle extends StatelessWidget {
  final _TrendRange value;
  final ValueChanged<_TrendRange> onChanged;
  const _RangeToggle({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    Widget seg(_TrendRange r, String label) {
      final selected = r == value;
      return InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: () => onChanged(r),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(color: selected ? AppColors.accent : Colors.transparent, borderRadius: BorderRadius.circular(8)),
          child: Text(label, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: selected ? Colors.white : AppColors.cardMuted)),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(color: AppColors.scaffoldBg, borderRadius: BorderRadius.circular(10)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [seg(_TrendRange.days7, '7D'), seg(_TrendRange.days30, '30D')]),
    );
  }
}

class _ChangePill extends StatelessWidget {
  final double current;
  final double previous;
  const _ChangePill({required this.current, required this.previous});

  @override
  Widget build(BuildContext context) {
    if (previous <= 0) return const SizedBox.shrink();
    final pct = ((current - previous) / previous) * 100;
    final up = pct >= 0;
    final color = up ? AppColors.warning : AppColors.success;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(up ? Icons.arrow_upward : Icons.arrow_downward, size: 12, color: color),
          const SizedBox(width: 2),
          Text('${pct.abs().toStringAsFixed(1)}%', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: color)),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final String? subtitle;
  const _StatCard({required this.label, required this.value, this.subtitle});

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
              if (subtitle != null) ...[
                const SizedBox(height: 2),
                Text(subtitle!, style: const TextStyle(fontSize: 11, color: AppColors.cardMuted)),
              ],
            ],
          ),
        ),
      );
}
