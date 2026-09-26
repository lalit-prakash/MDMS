import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../recharge/recharge_screen.dart';
import '../consumption/consumption_screen.dart';
import '../complaints/complaints_screen.dart';
import '../profile/profile_screen.dart';
import '../meter/meter_screen.dart';
import '../services/services_screen.dart';
import '../alerts/alerts_screen.dart';
import '../consumption/consumption_trend_card.dart';

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

class ConsumerSummary {
  final String accountNumber;
  final String name;
  final String? meterId;
  final String? meterNumber;
  final String connectionType;
  final double? walletBalance;
  final bool? isConnected;
  final double? lastRechargeAmount;
  final DateTime? lastRechargeAtUtc;

  ConsumerSummary({
    required this.accountNumber,
    required this.name,
    this.meterId,
    this.meterNumber,
    required this.connectionType,
    this.walletBalance,
    this.isConnected,
    this.lastRechargeAmount,
    this.lastRechargeAtUtc,
  });

  factory ConsumerSummary.fromJson(Map<String, dynamic> j) => ConsumerSummary(
        accountNumber: j['accountNumber'] as String,
        name: j['name'] as String,
        meterId: j['meterId'] as String?,
        meterNumber: j['meterNumber'] as String?,
        connectionType: j['connectionType'] as String,
        walletBalance: (j['walletBalance'] as num?)?.toDouble(),
        isConnected: j['isConnected'] as bool?,
        lastRechargeAmount: (j['lastRechargeAmount'] as num?)?.toDouble(),
        lastRechargeAtUtc: j['lastRechargeAtUtc'] != null ? DateTime.parse(j['lastRechargeAtUtc'] as String) : null,
      );
}

final summaryProvider = FutureProvider.autoDispose<ConsumerSummary>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/summary'));
  return ConsumerSummary.fromJson(response.data as Map<String, dynamic>);
});

/// Bottom navigation: Home / Consumption / Meter / Services / Profile — matching the reference
/// "Smart Electricity Consumer Portal" navigation structure (not just a recharge app).
class DashboardScreen extends ConsumerStatefulWidget {
  const DashboardScreen({super.key});

  @override
  ConsumerState<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends ConsumerState<DashboardScreen> {
  int _tab = 0;

  @override
  Widget build(BuildContext context) {
    final tabs = [const _HomeTab(), const ConsumptionScreen(), const MeterScreen(), const ServicesScreen(), const ProfileScreen()];
    return Scaffold(
      body: SafeArea(child: tabs[_tab]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (i) => setState(() => _tab = i),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home), label: 'Home'),
          NavigationDestination(icon: Icon(Icons.show_chart_outlined), selectedIcon: Icon(Icons.show_chart), label: 'Consumption'),
          NavigationDestination(icon: Icon(Icons.electric_meter_outlined), selectedIcon: Icon(Icons.electric_meter), label: 'Meter'),
          NavigationDestination(icon: Icon(Icons.grid_view_outlined), selectedIcon: Icon(Icons.grid_view), label: 'Services'),
          NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Profile'),
        ],
      ),
    );
  }
}

class _QuickAction {
  final String label;
  final IconData icon;
  final Color color;
  final WidgetBuilder builder;
  const _QuickAction(this.label, this.icon, this.color, this.builder);
}

class _HomeTab extends ConsumerWidget {
  const _HomeTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync = ref.watch(summaryProvider);
    final consumptionAsync = ref.watch(dailyConsumptionProvider);
    final meterAsync = ref.watch(meterOverviewProvider);

    final actions = [
      _QuickAction('Recharge', Icons.bolt, const Color(0xFF7C4DFF), (_) => const RechargeScreen()),
      _QuickAction('Consumption', Icons.show_chart, const Color(0xFF00BFA5), (_) => const ConsumptionScreen()),
      _QuickAction('Meter', Icons.electric_meter_outlined, const Color(0xFFFF8F00), (_) => const MeterScreen()),
      _QuickAction('Complaint', Icons.support_agent_outlined, const Color(0xFFE91E63), (_) => const ComplaintsScreen()),
    ];

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(summaryProvider);
        ref.invalidate(dailyConsumptionProvider);
        ref.invalidate(meterOverviewProvider);
      },
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              summaryAsync.maybeWhen(
                data: (s) => CircleAvatar(
                  radius: 22,
                  backgroundColor: AppColors.accent,
                  child: Text(s.name.isNotEmpty ? s.name[0].toUpperCase() : '?', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 18)),
                ),
                orElse: () => const CircleAvatar(radius: 22, child: Icon(Icons.person)),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    summaryAsync.when(
                      data: (s) => Text('Hello, ${s.name}', style: Theme.of(context).textTheme.headlineSmall),
                      loading: () => const Text('Loading...'),
                      error: (e, _) => Text(e.toString(), style: const TextStyle(color: Colors.red)),
                    ),
                    summaryAsync.maybeWhen(
                      data: (s) => Text('Consumer No: ${s.accountNumber}', style: const TextStyle(color: AppColors.cardMuted)),
                      orElse: () => const SizedBox.shrink(),
                    ),
                  ],
                ),
              ),
              Consumer(
                builder: (context, ref, _) {
                  final alertsAsync = ref.watch(alertsProvider);
                  final count = alertsAsync.maybeWhen(data: (rows) => rows.length, orElse: () => 0);
                  return IconButton(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const AlertsScreen())),
                    icon: Badge(
                      label: Text('$count'),
                      isLabelVisible: count > 0,
                      child: const Icon(Icons.notifications_outlined),
                    ),
                  );
                },
              ),
            ],
          ),
          const SizedBox(height: 16),
          summaryAsync.when(
            data: (s) => HeroGradientCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('WALLET BALANCE', style: TextStyle(fontSize: 12, letterSpacing: 1, color: Colors.white70)),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.18), borderRadius: BorderRadius.circular(20)),
                        child: Text(s.connectionType, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        s.walletBalance != null ? _currency.format(s.walletBalance) : 'No prepaid account',
                        style: const TextStyle(fontSize: 32, fontWeight: FontWeight.w700),
                      ),
                      const Expanded(child: _BalanceSparkline()),
                    ],
                  ),
                  if (s.isConnected == false) ...[
                    const SizedBox(height: 6),
                    const Text('Connection currently disconnected', style: TextStyle(color: Colors.orangeAccent, fontSize: 12)),
                  ],
                  if (s.lastRechargeAmount != null) ...[
                    const SizedBox(height: 10),
                    Container(height: 1, color: Colors.white24),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        const Icon(Icons.history, size: 16, color: Colors.white70),
                        const SizedBox(width: 6),
                        Text(
                          'Last Recharge: ${_currency.format(s.lastRechargeAmount)} on ${DateFormat.yMMMd().format(s.lastRechargeAtUtc!.toLocal())}',
                          style: const TextStyle(fontSize: 12, color: Colors.white70),
                        ),
                      ],
                    ),
                  ],
                  const SizedBox(height: 16),
                  SizedBox(
                    width: double.infinity,
                    child: FilledButton(
                      style: FilledButton.styleFrom(backgroundColor: Colors.white, foregroundColor: AppColors.accent),
                      onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RechargeScreen())),
                      child: const Text('RECHARGE'),
                    ),
                  ),
                ],
              ),
            ),
            loading: () => const SizedBox(height: 160, child: Center(child: CircularProgressIndicator(color: Colors.white))),
            error: (e, _) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(e.toString(), style: const TextStyle(color: Colors.red)))),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: consumptionAsync.when(
                  data: (rows) {
                    final today = rows.isNotEmpty ? rows.last.kwh : null;
                    final yesterday = rows.length > 1 ? rows[rows.length - 2].kwh : null;
                    final pctText = (today != null && yesterday != null && yesterday > 0)
                        ? '${(((today - yesterday) / yesterday) * 100).abs().toStringAsFixed(0)}% ${today >= yesterday ? "higher" : "lower"} than yesterday'
                        : null;
                    return StatTile(
                      label: "Today's Consumption",
                      value: today != null ? '${today.toStringAsFixed(2)} kWh' : '—',
                      subtitle: pctText,
                      subtitleColor: (today != null && yesterday != null && today > yesterday) ? AppColors.warning : AppColors.success,
                      icon: Icons.today_outlined,
                    );
                  },
                  loading: () => const StatTile(label: "Today's Consumption", value: '...', icon: Icons.today_outlined),
                  error: (e, _) => const StatTile(label: "Today's Consumption", value: '—', icon: Icons.today_outlined),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: meterAsync.when(
                  data: (m) => StatTile(
                    label: 'Maximum Demand',
                    value: m?.maximumDemandKw != null ? '${m!.maximumDemandKw!.toStringAsFixed(2)} kW' : '—',
                    subtitle: m?.sanctionedLoadKw != null ? 'Limit ${m!.sanctionedLoadKw!.toStringAsFixed(2)} kW' : null,
                    icon: Icons.speed_outlined,
                  ),
                  loading: () => const StatTile(label: 'Maximum Demand', value: '...', icon: Icons.speed_outlined),
                  error: (e, _) => const StatTile(label: 'Maximum Demand', value: '—', icon: Icons.speed_outlined),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Text('Quick Actions', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          GridView.count(
            crossAxisCount: 4,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: 8,
            crossAxisSpacing: 8,
            childAspectRatio: 0.85,
            children: actions
                .map((a) => InkWell(
                      borderRadius: BorderRadius.circular(16),
                      onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: a.builder)),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          CircleAvatar(radius: 22, backgroundColor: a.color.withValues(alpha: 0.14), child: Icon(a.icon, color: a.color)),
                          const SizedBox(height: 6),
                          Text(a.label, style: const TextStyle(fontSize: 11), textAlign: TextAlign.center),
                        ],
                      ),
                    ))
                .toList(),
          ),
          const SizedBox(height: 8),
          const ConsumptionTrendCard(),
          const SizedBox(height: 12),
          Consumer(
            builder: (context, ref, _) {
              final txAsync = ref.watch(transactionsProvider);
              return txAsync.maybeWhen(
                data: (rows) {
                  if (rows.isEmpty) return const SizedBox.shrink();
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text('Recent Transactions', style: Theme.of(context).textTheme.titleMedium),
                              TextButton(
                                onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RechargeScreen())),
                                child: const Text('View All'),
                              ),
                            ],
                          ),
                          for (final t in rows.take(3))
                            Padding(
                              padding: const EdgeInsets.symmetric(vertical: 6),
                              child: Row(
                                children: [
                                  CircleAvatar(
                                    radius: 16,
                                    backgroundColor: (t.amount >= 0 ? AppColors.success : AppColors.critical).withValues(alpha: 0.12),
                                    child: Icon(t.amount >= 0 ? Icons.add : Icons.remove, size: 16, color: t.amount >= 0 ? AppColors.success : AppColors.critical),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(t.type, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                                        Text(DateFormat.yMMMd().add_jm().format(t.createdAtUtc.toLocal()), style: const TextStyle(fontSize: 11, color: AppColors.cardMuted)),
                                      ],
                                    ),
                                  ),
                                  Text(
                                    '${t.amount >= 0 ? '+' : '-'}${_currency.format(t.amount.abs())}',
                                    style: TextStyle(fontWeight: FontWeight.w700, color: t.amount >= 0 ? AppColors.success : AppColors.critical),
                                  ),
                                ],
                              ),
                            ),
                        ],
                      ),
                    ),
                  );
                },
                orElse: () => const SizedBox.shrink(),
              );
            },
          ),
          const SizedBox(height: 12),
          consumptionAsync.maybeWhen(
            data: (rows) {
              final tip = _energySavingTip(rows);
              if (tip == null) return const SizedBox.shrink();
              return Card(
                color: AppColors.accent.withValues(alpha: 0.06),
                child: ListTile(
                  leading: const Icon(Icons.eco_outlined, color: AppColors.accent),
                  title: const Text('Energy Saving Tip', style: TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text(tip),
                ),
              );
            },
            orElse: () => const SizedBox.shrink(),
          ),
        ],
      ),
    );
  }

  /// A real, derived insight from the consumer's own last N days of Daily Load Profile
  /// consumption — never a fabricated or generic tip. Returns null when there isn't enough
  /// history yet to say anything meaningful (per the spec's own rule against overclaiming).
  String? _energySavingTip(List<DailyConsumptionRow> rows) {
    if (rows.length < 3) return null;
    final today = rows.last.kwh;
    final average = rows.reversed.skip(1).map((r) => r.kwh).fold<double>(0, (a, b) => a + b) / (rows.length - 1);
    if (average <= 0) return null;
    final pctDiff = ((today - average) / average) * 100;
    if (pctDiff.abs() < 10) return 'Your consumption today is close to your recent daily average.';
    return pctDiff > 0
        ? "Your consumption today is ${pctDiff.toStringAsFixed(0)}% higher than your recent daily average."
        : "Your consumption today is ${pctDiff.abs().toStringAsFixed(0)}% lower than your recent daily average.";
  }
}

/// A small inline trend of the wallet's own real balance history (from WalletTransaction.
/// BalanceAfter, oldest-to-newest of the last 10 entries) — never a decorative random squiggle.
class _BalanceSparkline extends ConsumerWidget {
  const _BalanceSparkline();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final txAsync = ref.watch(transactionsProvider);
    return txAsync.maybeWhen(
      data: (rows) {
        if (rows.length < 2) return const SizedBox.shrink();
        final ordered = rows.take(10).toList().reversed.toList();
        final spots = [for (var i = 0; i < ordered.length; i++) FlSpot(i.toDouble(), ordered[i].balanceAfter)];
        return SizedBox(
          height: 36,
          width: 90,
          child: LineChart(
            LineChartData(
              gridData: const FlGridData(show: false),
              borderData: FlBorderData(show: false),
              titlesData: const FlTitlesData(show: false),
              lineTouchData: const LineTouchData(enabled: false),
              lineBarsData: [
                LineChartBarData(
                  spots: spots,
                  isCurved: true,
                  color: Colors.white,
                  barWidth: 2,
                  dotData: const FlDotData(show: false),
                  belowBarData: BarAreaData(show: true, color: Colors.white.withValues(alpha: 0.15)),
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
