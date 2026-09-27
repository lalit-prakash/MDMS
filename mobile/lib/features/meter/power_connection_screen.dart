import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class PowerOutage {
  final DateTime failureAtUtc;
  final DateTime? restoredAtUtc;
  final int? durationMinutes;
  PowerOutage.fromJson(Map<String, dynamic> j)
      : failureAtUtc = DateTime.parse(j['failureAtUtc'] as String),
        restoredAtUtc = j['restoredAtUtc'] != null ? DateTime.parse(j['restoredAtUtc'] as String) : null,
        durationMinutes = j['durationMinutes'] as int?;
}

class ConnectionStatusEvent {
  final String type;
  final DateTime atUtc;
  final String? description;
  ConnectionStatusEvent.fromJson(Map<String, dynamic> j)
      : type = j['type'] as String,
        atUtc = DateTime.parse(j['atUtc'] as String),
        description = j['description'] as String?;
}

class PowerConnectionHistory {
  final bool? isConnected;
  final String? loadLimitState;
  final List<PowerOutage> outages;
  final List<ConnectionStatusEvent> connectionEvents;
  PowerConnectionHistory.fromJson(Map<String, dynamic> j)
      : isConnected = j['isConnected'] as bool?,
        loadLimitState = j['loadLimitState'] as String?,
        outages = (j['outages'] as List).map((e) => PowerOutage.fromJson(e as Map<String, dynamic>)).toList(),
        connectionEvents = (j['connectionEvents'] as List).map((e) => ConnectionStatusEvent.fromJson(e as Map<String, dynamic>)).toList();
}

final _powerConnectionProvider = FutureProvider.autoDispose<PowerConnectionHistory>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/power-connection-history'));
  return PowerConnectionHistory.fromJson(response.data as Map<String, dynamic>);
});

String _formatDuration(int minutes) {
  if (minutes < 60) return '$minutes min';
  final hours = minutes ~/ 60;
  final rem = minutes % 60;
  return rem == 0 ? '$hours hr' : '$hours hr $rem min';
}

/// Power outage history (real PowerFailure/PowerRestore MeterEvents, paired into outage periods)
/// and disconnection/reconnection history (real transitions in the meter's own Load Limit State
/// between consecutive Instantaneous Profile readings) -- this project has no separate remote
/// connect/disconnect audit log, so a genuine state change between two real meter readings is the
/// event, never an invented timeline.
class PowerConnectionScreen extends ConsumerWidget {
  const PowerConnectionScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final historyAsync = ref.watch(_powerConnectionProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Power & Connection')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(_powerConnectionProvider),
        child: historyAsync.when(
          data: (h) {
            final ongoingOutage = h.outages.any((o) => o.restoredAtUtc == null);
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Connection Status', style: TextStyle(fontSize: 12, color: AppColors.textTertiary)),
                              const SizedBox(height: 4),
                              Text(
                                ongoingOutage ? 'Power Outage' : (h.isConnected == false ? 'Disconnected' : 'Connected'),
                                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                              ),
                              if (h.loadLimitState != null && h.loadLimitState != 'Normal') ...[
                                const SizedBox(height: 2),
                                Text('Load limit state: ${h.loadLimitState}', style: const TextStyle(fontSize: 12, color: AppColors.textTertiary)),
                              ],
                            ],
                          ),
                        ),
                        StatusPill(
                          text: ongoingOutage ? 'OUTAGE' : (h.isConnected == false ? 'DISCONNECTED' : 'NORMAL'),
                          color: ongoingOutage || h.isConnected == false ? AppColors.critical : AppColors.success,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 20),
                Text('Power Outage History', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                if (h.outages.isEmpty)
                  const Padding(padding: EdgeInsets.symmetric(vertical: 12), child: Text('No power outages recorded.', style: TextStyle(color: AppColors.textTertiary)))
                else
                  ...h.outages.map((o) => Card(
                        child: ListTile(
                          leading: CircleAvatar(
                            backgroundColor: (o.restoredAtUtc == null ? AppColors.critical : AppColors.warning).withValues(alpha: 0.12),
                            child: Icon(Icons.power_off, color: o.restoredAtUtc == null ? AppColors.critical : AppColors.warning, size: 18),
                          ),
                          title: Text(DateFormat.yMMMd().add_jm().format(o.failureAtUtc.toLocal())),
                          subtitle: Text(o.restoredAtUtc != null
                              ? 'Restored ${DateFormat.yMMMd().add_jm().format(o.restoredAtUtc!.toLocal())} · ${_formatDuration(o.durationMinutes!)}'
                              : 'Still ongoing'),
                          trailing: o.restoredAtUtc == null ? const StatusPill(text: 'ONGOING', color: AppColors.critical) : null,
                        ),
                      )),
                const SizedBox(height: 20),
                Text('Disconnection / Reconnection History', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                if (h.connectionEvents.isEmpty)
                  const Padding(padding: EdgeInsets.symmetric(vertical: 12), child: Text('No disconnection or reconnection events recorded.', style: TextStyle(color: AppColors.textTertiary)))
                else
                  ...h.connectionEvents.map((e) {
                    final isReconnect = e.type == 'Normal';
                    final color = isReconnect ? AppColors.success : (e.type == 'Disconnected' ? AppColors.critical : AppColors.warning);
                    return Card(
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: color.withValues(alpha: 0.12),
                          child: Icon(isReconnect ? Icons.link : Icons.link_off, color: color, size: 18),
                        ),
                        title: Text(e.description ?? e.type),
                        subtitle: Text(DateFormat.yMMMd().add_jm().format(e.atUtc.toLocal())),
                      ),
                    );
                  }),
              ],
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: AppColors.critical)))),
        ),
      ),
    );
  }
}
