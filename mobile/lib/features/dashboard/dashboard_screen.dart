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
import '../meter_testing/meter_testing_screen.dart';
import '../services/services_screen.dart';
import '../alerts/alerts_screen.dart';
import '../consumption/consumption_trend_card.dart';
import '../consumption/environmental_impact_card.dart';

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

/// Today's real peak half-hour usage (expressed as an average kW over that interval, same
/// derivation as the Consumption day-detail screen) -- distinct from Maximum Demand, which is the
/// meter's own recorded MD for the current billing month.
final _todayPeakProvider = FutureProvider.autoDispose<double?>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final today = DateTime.now();
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/consumption/day-detail',
      queryParameters: {'date': '${today.year.toString().padLeft(4, '0')}-${today.month.toString().padLeft(2, '0')}-${today.day.toString().padLeft(2, '0')}'}));
  final data = response.data as Map<String, dynamic>;
  return (data['peakKw'] as num?)?.toDouble();
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
    final consumptionAsync = ref.watch(dailyConsumptionProvider(7));
    final meterAsync = ref.watch(meterOverviewProvider);

    final actions = [
      _QuickAction('Recharge', Icons.bolt, const Color(0xFF7C4DFF), (_) => const RechargeScreen()),
      _QuickAction('Consumption', Icons.show_chart, const Color(0xFF00BFA5), (_) => const ConsumptionScreen()),
      _QuickAction('Meter', Icons.electric_meter_outlined, const Color(0xFFFF8F00), (_) => const MeterScreen()),
      _QuickAction('Meter Testing', Icons.fact_check_outlined, const Color(0xFF3F51B5), (_) => const MeterTestingScreen()),
      _QuickAction('Complaint', Icons.support_agent_outlined, const Color(0xFFE91E63), (_) => const ComplaintsScreen()),
    ];

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(summaryProvider);
        ref.invalidate(dailyConsumptionProvider(7));
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
                  Text(
                    s.walletBalance != null ? _currency.format(s.walletBalance) : 'No prepaid account',
                    style: const TextStyle(fontSize: 32, fontWeight: FontWeight.w700),
                  ),
                  if (s.isConnected == false) ...[
                    const SizedBox(height: 6),
                    const Text('Connection currently disconnected', style: TextStyle(color: Colors.orangeAccent, fontSize: 12)),
                  ],
                  Consumer(
                    builder: (context, ref, _) {
                      final txAsync = ref.watch(transactionsProvider);
                      return txAsync.maybeWhen(
                        data: (rows) {
                          if (rows.isEmpty || s.walletBalance == null) return const SizedBox.shrink();
                          final debits = rows.where((t) => t.type == 'ConsumptionDebit').toList();
                          double? estimatedDays;
                          if (debits.length >= 2) {
                            final recent = debits.take(7).toList();
                            final avgDaily = recent.fold<double>(0, (a, t) => a + t.amount.abs()) / recent.length;
                            if (avgDaily > 0) estimatedDays = s.walletBalance! / avgDaily;
                          }
                          final lastDeduction = debits.isNotEmpty ? debits.first : null;
                          return Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              if (estimatedDays != null) ...[
                                const SizedBox(height: 4),
                                Row(
                                  children: [
                                    Text('≈ ${estimatedDays.toStringAsFixed(1)} days estimated balance', style: const TextStyle(fontSize: 12, color: Colors.white70)),
                                    const SizedBox(width: 4),
                                    Tooltip(
                                      message: 'Estimated from your average daily consumption debit over the last ${debits.take(7).length} days.',
                                      child: const Icon(Icons.info_outline, size: 13, color: Colors.white54),
                                    ),
                                  ],
                                ),
                              ],
                              if (s.lastRechargeAmount != null || lastDeduction != null) ...[
                                const SizedBox(height: 10),
                                Container(height: 1, color: Colors.white24),
                                const SizedBox(height: 10),
                              ],
                              if (s.lastRechargeAmount != null)
                                Padding(
                                  padding: const EdgeInsets.only(bottom: 4),
                                  child: Row(
                                    children: [
                                      const Icon(Icons.arrow_downward, size: 14, color: Colors.white70),
                                      const SizedBox(width: 6),
                                      Text(
                                        'Last Recharge: ${_currency.format(s.lastRechargeAmount)} on ${DateFormat.yMMMd().format(s.lastRechargeAtUtc!.toLocal())}',
                                        style: const TextStyle(fontSize: 12, color: Colors.white70),
                                      ),
                                    ],
                                  ),
                                ),
                              if (lastDeduction != null)
                                Row(
                                  children: [
                                    const Icon(Icons.arrow_upward, size: 14, color: Colors.white70),
                                    const SizedBox(width: 6),
                                    Text(
                                      'Last Deduction: ${_currency.format(lastDeduction.amount.abs())} on ${DateFormat.yMMMd().format(lastDeduction.createdAtUtc.toLocal())}',
                                      style: const TextStyle(fontSize: 12, color: Colors.white70),
                                    ),
                                  ],
                                ),
                            ],
                          );
                        },
                        orElse: () => const SizedBox.shrink(),
                      );
                    },
                  ),
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
                child: Consumer(
                  builder: (context, ref, _) {
                    final peakAsync = ref.watch(_todayPeakProvider);
                    return peakAsync.when(
                      data: (peak) => StatTile(label: "Today's Peak Usage", value: peak != null ? '${peak.toStringAsFixed(2)} kW' : '—', icon: Icons.bolt_outlined),
                      loading: () => const StatTile(label: "Today's Peak Usage", value: '...', icon: Icons.bolt_outlined),
                      error: (e, _) => const StatTile(label: "Today's Peak Usage", value: '—', icon: Icons.bolt_outlined),
                    );
                  },
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          meterAsync.maybeWhen(
            data: (m) {
              if (m?.maximumDemandKw == null || m?.sanctionedLoadKw == null || m!.sanctionedLoadKw! <= 0) return const SizedBox.shrink();
              final ratio = (m.maximumDemandKw! / m.sanctionedLoadKw!).clamp(0.0, 1.5);
              final breach = ratio > 1.0;
              final warning = ratio > 0.9;
              final barColor = breach ? AppColors.critical : (warning ? AppColors.warning : AppColors.success);
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.bar_chart, color: AppColors.accent, size: 18),
                          const SizedBox(width: 8),
                          const Expanded(child: Text('Maximum Demand (Current Month)', style: TextStyle(fontWeight: FontWeight.w600))),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                            decoration: BoxDecoration(color: barColor.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
                            child: Text('${(ratio * 100).toStringAsFixed(0)}%', style: TextStyle(color: barColor, fontSize: 11, fontWeight: FontWeight.w700)),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text('${m.maximumDemandKw!.toStringAsFixed(2)} kW / ${m.sanctionedLoadKw!.toStringAsFixed(2)} kW', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 8),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(6),
                        child: LinearProgressIndicator(value: ratio.clamp(0.0, 1.0), minHeight: 8, backgroundColor: AppColors.scaffoldBg, color: barColor),
                      ),
                      if (breach || warning) ...[
                        const SizedBox(height: 8),
                        Row(
                          children: [
                            Icon(Icons.warning_amber_rounded, size: 14, color: barColor),
                            const SizedBox(width: 4),
                            Text(breach ? 'Demand limit exceeded' : 'Approaching your demand limit', style: TextStyle(color: barColor, fontSize: 12, fontWeight: FontWeight.w600)),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
              );
            },
            orElse: () => const SizedBox.shrink(),
          ),
          const SizedBox(height: 12),
          meterAsync.maybeWhen(
            data: (m) {
              if (m == null) return const SizedBox.shrink();
              final connected = m.status == 'Installed';
              return Card(
                child: ListTile(
                  leading: Icon(Icons.sensors, color: connected ? AppColors.success : AppColors.critical),
                  title: Text(connected ? 'Meter Connected' : 'Meter Communication Issue', style: const TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text(m.latestProfileTimeUtc != null ? 'Last data received: ${DateFormat.yMMMd().add_jm().format(m.latestProfileTimeUtc!.toLocal())}' : 'No profile data received yet'),
                ),
              );
            },
            orElse: () => const SizedBox.shrink(),
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
          Consumer(
            builder: (context, ref, _) {
              final alertsAsync = ref.watch(alertsProvider);
              return alertsAsync.maybeWhen(
                data: (rows) {
                  final important = rows.where((a) => a.severity == 'Critical' || a.severity == 'High').toList();
                  if (important.isEmpty) return const SizedBox.shrink();
                  final top = important.first;
                  final color = top.severity == 'Critical' ? AppColors.critical : AppColors.warning;
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: InkWell(
                      borderRadius: BorderRadius.circular(16),
                      onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const AlertsScreen())),
                      child: Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(color: color.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(16)),
                        child: Row(
                          children: [
                            CircleAvatar(radius: 18, backgroundColor: color.withValues(alpha: 0.16), child: Icon(Icons.notifications_active, color: color, size: 18)),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('Important Alert', style: TextStyle(color: color, fontWeight: FontWeight.w700, fontSize: 12)),
                                  Text(top.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                                  Text(top.message, style: const TextStyle(fontSize: 12, color: AppColors.cardMuted), maxLines: 2, overflow: TextOverflow.ellipsis),
                                ],
                              ),
                            ),
                            Icon(Icons.chevron_right, color: color),
                          ],
                        ),
                      ),
                    ),
                  );
                },
                orElse: () => const SizedBox.shrink(),
              );
            },
          ),
          const EnvironmentalImpactCard(),
          const SizedBox(height: 12),
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
