import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

enum _TrendRange { today, days7, days30 }
enum _TrendUnit { kwh, inr }

extension on _TrendRange {
  String get apiValue => switch (this) { _TrendRange.today => 'today', _TrendRange.days7 => '7days', _TrendRange.days30 => '30days' };
  String get label => switch (this) { _TrendRange.today => 'Today', _TrendRange.days7 => '7 Days', _TrendRange.days30 => '30 Days' };
}

class TrendPoint {
  final DateTime atUtc;
  final double kwh;
  final double? inr;
  TrendPoint.fromJson(Map<String, dynamic> j)
      : atUtc = DateTime.parse(j['atUtc'] as String),
        kwh = (j['kwh'] as num).toDouble(),
        inr = (j['inr'] as num?)?.toDouble();
}

class TrendResponse {
  final List<TrendPoint> points;
  final TrendPoint? maxKwh;
  final TrendPoint? minKwh;
  final TrendPoint? maxInr;
  final TrendPoint? minInr;

  TrendResponse.fromJson(Map<String, dynamic> j)
      : points = (j['points'] as List).map((e) => TrendPoint.fromJson(e as Map<String, dynamic>)).toList(),
        maxKwh = j['maxKwhPoint'] != null ? TrendPoint.fromJson(j['maxKwhPoint'] as Map<String, dynamic>) : null,
        minKwh = j['minKwhPoint'] != null ? TrendPoint.fromJson(j['minKwhPoint'] as Map<String, dynamic>) : null,
        maxInr = j['maxInrPoint'] != null ? TrendPoint.fromJson(j['maxInrPoint'] as Map<String, dynamic>) : null,
        minInr = j['minInrPoint'] != null ? TrendPoint.fromJson(j['minInrPoint'] as Map<String, dynamic>) : null;
}

final _trendProvider = FutureProvider.autoDispose.family<TrendResponse, String>((ref, range) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/trend', queryParameters: {'range': range}));
  return TrendResponse.fromJson(response.data as Map<String, dynamic>);
});

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

/// Consumption Trend card for the Home dashboard: Today / 7 Days / 30 Days, each viewable as
/// real kWh (Load Survey / Daily Load Profile) or real INR (the actual amount this project's own
/// daily-billing run debited from the wallet for that day — never an estimated unit cost). A day
/// with no billing run simply has no INR point; if none of the visible days have one, the INR
/// view says so instead of showing an empty chart.
class ConsumptionTrendCard extends ConsumerStatefulWidget {
  const ConsumptionTrendCard({super.key});

  @override
  ConsumerState<ConsumptionTrendCard> createState() => _ConsumptionTrendCardState();
}

class _ConsumptionTrendCardState extends ConsumerState<ConsumptionTrendCard> {
  _TrendRange _range = _TrendRange.today;
  _TrendUnit _unit = _TrendUnit.kwh;

  @override
  Widget build(BuildContext context) {
    final trendAsync = ref.watch(_trendProvider(_range.apiValue));

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Consumption Trend', style: Theme.of(context).textTheme.titleMedium),
                SegmentedButton<_TrendUnit>(
                  segments: const [
                    ButtonSegment(value: _TrendUnit.kwh, label: Text('kWh')),
                    ButtonSegment(value: _TrendUnit.inr, label: Text('₹')),
                  ],
                  selected: {_unit},
                  showSelectedIcon: false,
                  onSelectionChanged: (s) => setState(() => _unit = s.first),
                  style: const ButtonStyle(visualDensity: VisualDensity.compact),
                ),
              ],
            ),
            const SizedBox(height: 10),
            SegmentedButton<_TrendRange>(
              segments: _TrendRange.values.map((r) => ButtonSegment(value: r, label: Text(r.label))).toList(),
              selected: {_range},
              showSelectedIcon: false,
              onSelectionChanged: (s) => setState(() => _range = s.first),
            ),
            const SizedBox(height: 16),
            trendAsync.when(
              data: (trend) => _buildBody(trend),
              loading: () => const SizedBox(height: 160, child: Center(child: CircularProgressIndicator())),
              error: (e, _) => Padding(padding: const EdgeInsets.all(8), child: Text(e.toString(), style: const TextStyle(color: AppColors.critical))),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBody(TrendResponse trend) {
    final isInr = _unit == _TrendUnit.inr;
    final spotSource = isInr ? trend.points.where((p) => p.inr != null).toList() : trend.points;

    if (spotSource.isEmpty) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 24),
        child: Text(
          isInr ? 'No billing amount recorded yet for this period.' : 'No consumption data available for this period.',
          style: const TextStyle(color: AppColors.cardMuted),
        ),
      );
    }

    final spots = [
      for (var i = 0; i < spotSource.length; i++) FlSpot(i.toDouble(), (isInr ? spotSource[i].inr! : spotSource[i].kwh)),
    ];
    final maxPoint = isInr ? trend.maxInr : trend.maxKwh;
    final minPoint = isInr ? trend.minInr : trend.minKwh;
    String formatValue(double v) => isInr ? _currency.format(v) : '${v.toStringAsFixed(2)} kWh';
    final dateFormat = _range == _TrendRange.today ? DateFormat.Hm() : DateFormat.MMMd();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 180,
          child: LineChart(
            LineChartData(
              gridData: const FlGridData(show: true, drawVerticalLine: false),
              borderData: FlBorderData(show: false),
              titlesData: FlTitlesData(
                topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                leftTitles: AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: isInr ? 44 : 36)),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 28,
                    getTitlesWidget: (value, meta) {
                      final i = value.toInt();
                      if (i < 0 || i >= spotSource.length) return const SizedBox.shrink();
                      final step = (spotSource.length / 4).ceil().clamp(1, spotSource.length);
                      if (i % step != 0) return const SizedBox.shrink();
                      return Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Text(dateFormat.format(spotSource[i].atUtc.toLocal()), style: const TextStyle(fontSize: 9)),
                      );
                    },
                  ),
                ),
              ),
              lineBarsData: [
                LineChartBarData(
                  spots: spots,
                  isCurved: true,
                  color: AppColors.accent,
                  barWidth: 2,
                  dotData: const FlDotData(show: false),
                  belowBarData: BarAreaData(show: true, color: AppColors.accent.withValues(alpha: 0.08)),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            if (maxPoint != null)
              Expanded(
                child: StatTile(
                  label: 'Maximum',
                  value: formatValue(isInr ? maxPoint.inr! : maxPoint.kwh),
                  subtitle: dateFormat.format(maxPoint.atUtc.toLocal()),
                  icon: Icons.trending_up,
                ),
              ),
            if (maxPoint != null && minPoint != null) const SizedBox(width: 12),
            if (minPoint != null)
              Expanded(
                child: StatTile(
                  label: 'Minimum',
                  value: formatValue(isInr ? minPoint.inr! : minPoint.kwh),
                  subtitle: dateFormat.format(minPoint.atUtc.toLocal()),
                  icon: Icons.trending_down,
                ),
              ),
          ],
        ),
      ],
    );
  }
}
