import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class AlertRow {
  final String category;
  final String severity;
  final String title;
  final String message;
  final DateTime atUtc;

  AlertRow.fromJson(Map<String, dynamic> j)
      : category = j['category'] as String,
        severity = j['severity'] as String,
        title = j['title'] as String,
        message = j['message'] as String,
        atUtc = DateTime.parse(j['atUtc'] as String);
}

final alertsProvider = FutureProvider.autoDispose<List<AlertRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/alerts'));
  return (response.data as List).map((e) => AlertRow.fromJson(e as Map<String, dynamic>)).toList();
});

Color _severityColor(String severity) {
  switch (severity) {
    case 'Critical':
      return AppColors.critical;
    case 'High':
      return AppColors.warning;
    default:
      return AppColors.accent;
  }
}

IconData _categoryIcon(String category) {
  switch (category) {
    case 'Wallet':
      return Icons.account_balance_wallet_outlined;
    case 'Meter':
      return Icons.electric_meter_outlined;
    case 'Complaint':
      return Icons.support_agent_outlined;
    default:
      return Icons.notifications_outlined;
  }
}

/// Alert & Notification Centre — assembled on demand from real signals (wallet balance, meter
/// communication recency, meter events, unresolved complaints) via GET .../alerts. There is no
/// stored/pushed notification log or FCM wiring in this project, so there is no read/unread state
/// or push delivery — this is always the current live picture, not a history.
class AlertsScreen extends ConsumerWidget {
  const AlertsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final alertsAsync = ref.watch(alertsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Alerts & Notifications')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(alertsProvider),
        child: alertsAsync.when(
          data: (rows) => rows.isEmpty
              ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No active alerts. Everything looks normal.'))])
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: rows.length,
                  itemBuilder: (context, i) {
                    final a = rows[i];
                    return Card(
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: _severityColor(a.severity).withValues(alpha: 0.12),
                          child: Icon(_categoryIcon(a.category), color: _severityColor(a.severity)),
                        ),
                        title: Text(a.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text('${a.message}\n${DateFormat.yMMMd().add_jm().format(a.atUtc.toLocal())}'),
                        isThreeLine: true,
                        trailing: StatusPill(text: a.severity, color: _severityColor(a.severity)),
                      ),
                    );
                  },
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: Colors.red)))),
        ),
      ),
    );
  }
}
