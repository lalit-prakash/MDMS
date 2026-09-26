import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../recharge/recharge_screen.dart';
import '../consumption/consumption_screen.dart';
import '../complaints/complaints_screen.dart';
import '../profile/profile_screen.dart';
import '../bills/bills_screen.dart';

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

class ConsumerSummary {
  final String accountNumber;
  final String name;
  final String? meterNumber;
  final double? walletBalance;
  final bool? isConnected;

  ConsumerSummary({
    required this.accountNumber,
    required this.name,
    this.meterNumber,
    this.walletBalance,
    this.isConnected,
  });

  factory ConsumerSummary.fromJson(Map<String, dynamic> j) => ConsumerSummary(
        accountNumber: j['accountNumber'] as String,
        name: j['name'] as String,
        meterNumber: j['meterNumber'] as String?,
        walletBalance: (j['walletBalance'] as num?)?.toDouble(),
        isConnected: j['isConnected'] as bool?,
      );
}

final summaryProvider = FutureProvider.autoDispose<ConsumerSummary>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/summary'));
  return ConsumerSummary.fromJson(response.data as Map<String, dynamic>);
});

class DashboardScreen extends ConsumerStatefulWidget {
  const DashboardScreen({super.key});

  @override
  ConsumerState<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends ConsumerState<DashboardScreen> {
  int _tab = 0;

  @override
  Widget build(BuildContext context) {
    final tabs = [const _HomeTab(), const ConsumptionScreen(), const ComplaintsScreen(), const ProfileScreen()];
    return Scaffold(
      body: SafeArea(child: tabs[_tab]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (i) => setState(() => _tab = i),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home), label: 'Home'),
          NavigationDestination(icon: Icon(Icons.bolt_outlined), selectedIcon: Icon(Icons.bolt), label: 'Consumption'),
          NavigationDestination(icon: Icon(Icons.support_agent_outlined), selectedIcon: Icon(Icons.support_agent), label: 'Complaints'),
          NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: 'Profile'),
        ],
      ),
    );
  }
}

class _HomeTab extends ConsumerWidget {
  const _HomeTab();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync = ref.watch(summaryProvider);

    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(summaryProvider),
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Good day,', style: Theme.of(context).textTheme.titleMedium),
          summaryAsync.when(
            data: (s) => Text(s.name, style: Theme.of(context).textTheme.headlineSmall),
            loading: () => const Text('Loading...'),
            error: (e, _) => Text(e.toString(), style: const TextStyle(color: Colors.red)),
          ),
          const SizedBox(height: 16),
          summaryAsync.when(
            data: (s) => _WalletCard(summary: s),
            loading: () => const _LoadingCard(),
            error: (e, _) => _ErrorCard(message: e.toString(), onRetry: () => ref.invalidate(summaryProvider)),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ConsumptionScreen())),
                  icon: const Icon(Icons.show_chart),
                  label: const Text('Consumption'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ComplaintsScreen())),
                  icon: const Icon(Icons.support_agent),
                  label: const Text('Complaints'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const BillsScreen())),
            icon: const Icon(Icons.receipt_long_outlined),
            label: const Text('Bills & Statements'),
          ),
          const SizedBox(height: 16),
          summaryAsync.maybeWhen(
            data: (s) => Card(
              child: ListTile(
                leading: const Icon(Icons.electric_meter_outlined),
                title: const Text('Meter Number'),
                subtitle: Text(s.meterNumber ?? 'No meter currently assigned'),
              ),
            ),
            orElse: () => const SizedBox.shrink(),
          ),
        ],
      ),
    );
  }
}

class _WalletCard extends StatelessWidget {
  final ConsumerSummary summary;
  const _WalletCard({required this.summary});

  @override
  Widget build(BuildContext context) {
    final hasWallet = summary.walletBalance != null;
    return Card(
      color: Theme.of(context).colorScheme.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Wallet Balance'),
            const SizedBox(height: 4),
            Text(
              hasWallet ? _currency.format(summary.walletBalance) : 'No prepaid account',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            if (summary.isConnected == false) ...[
              const SizedBox(height: 8),
              const Text('Connection currently disconnected', style: TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RechargeScreen())),
              child: const Text('RECHARGE NOW'),
            ),
          ],
        ),
      ),
    );
  }
}

class _LoadingCard extends StatelessWidget {
  const _LoadingCard();
  @override
  Widget build(BuildContext context) => const Card(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Center(child: CircularProgressIndicator()),
        ),
      );
}

class _ErrorCard extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorCard({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              Text(message, style: const TextStyle(color: Colors.red)),
              const SizedBox(height: 8),
              TextButton(onPressed: onRetry, child: const Text('Retry')),
            ],
          ),
        ),
      );
}
