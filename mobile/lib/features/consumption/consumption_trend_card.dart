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
  String get label => switch (this) { _TrendRange.today => 'Today', _TrendRange.days7 => 'L7D', _TrendRange.days30 => 'L30D' };
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

class EnvironmentalImpact {
  final double totalKwh;
  final double totalPowerOnHours;
  final double co2Kg;
  final double treesSaved;
  EnvironmentalImpact.fromJson(Map<String, dynamic> j)
      : totalKwh = (j['totalKwh'] as num).toDouble(),
        totalPowerOnHours = (j['totalPowerOnHours'] as num).toDouble(),
        co2Kg = (j['co2Kg'] as num).toDouble(),
        treesSaved = (j['treesSaved'] as num).toDouble();
}

class TrendResponse {
  final List<TrendPoint> points;
  final TrendPoint? maxKwh;
  final TrendPoint? minKwh;
  final TrendPoint? maxInr;
  final TrendPoint? minInr;
  final EnvironmentalImpact impact;

  TrendResponse.fromJson(Map<String, dynamic> j)
      : points = (j['points'] as List).map((e) => TrendPoint.fromJson(e as Map<String, dynamic>)).toList(),
        maxKwh = j['maxKwhPoint'] != null ? TrendPoint.fromJson(j['maxKwhPoint'] as Map<String, dynamic>) : null,
        minKwh = j['minKwhPoint'] != null ? TrendPoint.fromJson(j['minKwhPoint'] as Map<String, dynamic>) : null,
        maxInr = j['maxInrPoint'] != null ? TrendPoint.fromJson(j['maxInrPoint'] as Map<String, dynamic>) : null,
        minInr = j['minInrPoint'] != null ? TrendPoint.fromJson(j['minInrPoint'] as Map<String, dynamic>) : null,
        impact = EnvironmentalImpact.fromJson(j['impact'] as Map<String, dynamic>);
}

final _trendProvider = FutureProvider.autoDispose.family<TrendResponse, String>((ref, range) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/trend', queryParameters: {'range': range}));
  return TrendResponse.fromJson(response.data as Map<String, dynamic>);
});

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

/// Consumption Trend card for the Home dashboard: Today / L7D / L30D, viewable as real kWh
/// (Load Survey / Daily Load Profile) or real INR (the actual amount this project's own
/// daily-billing run debited from the wallet for that day -- never an estimated unit cost). A
/// day with no billing run simply has no INR point.
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
              children: [
                const Icon(Icons.bolt, color: AppColors.accent, size: 20),
                const SizedBox(width: 8),
                Text('Consumption Trend', style: Theme.of(context).textTheme.titleMedium),
              ],
            ),
            const SizedBox(height: 12),
            _ToggleBar<_TrendUnit>(
              value: _unit,
              options: const {_TrendUnit.kwh: 'Units', _TrendUnit.inr: 'Money'},
              onChanged: (v) => setState(() => _unit = v),
            ),
            const SizedBox(height: 16),
            trendAsync.when(
              data: (trend) => _buildBody(trend),
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
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

    final total = spotSource.fold<double>(0, (a, p) => a + (isInr ? p.inr! : p.kwh));
    final avg = spotSource.isEmpty ? 0.0 : total / spotSource.length;
    double? changePct;
    if (spotSource.length >= 2) {
      final last = isInr ? spotSource.last.inr! : spotSource.last.kwh;
      final prevAvg = spotSource
              .sublist(0, spotSource.length - 1)
              .fold<double>(0, (a, p) => a + (isInr ? p.inr! : p.kwh)) /
          (spotSource.length - 1);
      if (prevAvg > 0) changePct = ((last - prevAvg) / prevAvg) * 100;
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Expanded(
              child: Wrap(
                crossAxisAlignment: WrapCrossAlignment.end,
                spacing: 10,
                runSpacing: 4,
                children: [
                  Text(
                    isInr ? _currency.format(total) : '${total.toStringAsFixed(2)} Units',
                    style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800),
                  ),
                  if (changePct != null)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: (changePct >= 0 ? AppColors.warning : AppColors.success).withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(changePct >= 0 ? Icons.arrow_upward : Icons.arrow_downward, size: 12, color: changePct >= 0 ? AppColors.warning : AppColors.success),
                          const SizedBox(width: 2),
                          Text('${changePct.abs().toStringAsFixed(1)}%',
                              style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: changePct >= 0 ? AppColors.warning : AppColors.success)),
                        ],
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            _ToggleBar<_TrendRange>(
              value: _range,
              options: {for (final r in _TrendRange.values) r: r.label},
              onChanged: (v) => setState(() => _range = v),
              compact: true,
            ),
          ],
        ),
        const SizedBox(height: 16),
        if (spotSource.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Text(
              isInr ? 'No billing amount recorded yet for this period.' : 'No consumption data available for this period.',
              style: const TextStyle(color: AppColors.cardMuted),
            ),
          )
        else ...[
          _buildChart(spotSource, isInr, avg),
          const SizedBox(height: 16),
          _buildMinMax(trend, isInr),
        ],
      ],
    );
  }

  Widget _buildMinMax(TrendResponse trend, bool isInr) {
    final maxPoint = isInr ? trend.maxInr : trend.maxKwh;
    final minPoint = isInr ? trend.minInr : trend.minKwh;
    if (maxPoint == null && minPoint == null) return const SizedBox.shrink();
    final dateFormat = _range == _TrendRange.today ? DateFormat.Hm() : DateFormat.MMMd();
    String formatValue(double v) => isInr ? _currency.format(v) : '${v.toStringAsFixed(2)} kWh';
    return Row(
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
    );
  }

  Widget _buildChart(List<TrendPoint> spotSource, bool isInr, double avg) {
    final dateFormat = _range == _TrendRange.today ? DateFormat.Hm() : DateFormat.MMMd();
    final values = spotSource.map((p) => isInr ? p.inr! : p.kwh).toList();
    final maxY = values.reduce((a, b) => a > b ? a : b) * 1.2;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 180,
          child: BarChart(
            BarChartData(
              maxY: maxY == 0 ? 1 : maxY,
              gridData: const FlGridData(show: true, drawVerticalLine: false),
              borderData: FlBorderData(show: false),
              extraLinesData: ExtraLinesData(horizontalLines: [
                HorizontalLine(y: avg, color: AppColors.cardMuted, strokeWidth: 1, dashArray: [6, 4]),
              ]),
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
                      final step = (spotSource.length / 6).ceil().clamp(1, spotSource.length);
                      if (i % step != 0) return const SizedBox.shrink();
                      return Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Text(dateFormat.format(spotSource[i].atUtc.toLocal()), style: const TextStyle(fontSize: 9)),
                      );
                    },
                  ),
                ),
              ),
              barGroups: [
                for (var i = 0; i < spotSource.length; i++)
                  BarChartGroupData(x: i, barRods: [
                    BarChartRodData(toY: values[i], color: AppColors.accent, width: 8, borderRadius: BorderRadius.circular(3)),
                  ]),
              ],
            ),
          ),
        ),
        const SizedBox(height: 4),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(width: 10, height: 10, decoration: const BoxDecoration(color: AppColors.accent, shape: BoxShape.circle)),
            const SizedBox(width: 6),
            const Text('Consumption', style: TextStyle(fontSize: 11, color: AppColors.cardMuted)),
            const SizedBox(width: 16),
            Container(width: 14, height: 0, decoration: const BoxDecoration(border: Border(top: BorderSide(color: AppColors.cardMuted, width: 1)))),
            const SizedBox(width: 6),
            const Text('Average', style: TextStyle(fontSize: 11, color: AppColors.cardMuted)),
          ],
        ),
      ],
    );
  }
}

class _ToggleBar<T> extends StatelessWidget {
  final T value;
  final Map<T, String> options;
  final ValueChanged<T> onChanged;
  final bool compact;
  const _ToggleBar({required this.value, required this.options, required this.onChanged, this.compact = false, super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(color: AppColors.scaffoldBg, borderRadius: BorderRadius.circular(compact ? 10 : 12)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: options.entries.map((e) {
          final selected = e.key == value;
          return InkWell(
            borderRadius: BorderRadius.circular(compact ? 8 : 10),
            onTap: () => onChanged(e.key),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              padding: EdgeInsets.symmetric(horizontal: compact ? 10 : 18, vertical: compact ? 6 : 9),
              decoration: BoxDecoration(
                color: selected ? AppColors.accent : Colors.transparent,
                borderRadius: BorderRadius.circular(compact ? 8 : 10),
              ),
              child: Text(
                e.value,
                style: TextStyle(
                  fontSize: compact ? 11 : 13,
                  fontWeight: FontWeight.w600,
                  color: selected ? Colors.white : AppColors.cardMuted,
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}
