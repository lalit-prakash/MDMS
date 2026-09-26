import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../dashboard/dashboard_screen.dart';

class MeterTestingRow {
  final String id;
  final String requestNumber;
  final String reason;
  final String status;
  final DateTime createdAtUtc;
  final String? testResult;
  final String? accuracyResult;

  MeterTestingRow.fromJson(Map<String, dynamic> j)
      : id = j['id'] as String,
        requestNumber = j['requestNumber'] as String,
        reason = j['reason'] as String,
        status = j['status'] as String,
        createdAtUtc = DateTime.parse(j['createdAtUtc'] as String),
        testResult = j['testResult'] as String?,
        accuracyResult = j['accuracyResult'] as String?;
}

final meterTestingListProvider = FutureProvider.autoDispose<List<MeterTestingRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/meter-testing', queryParameters: {'customerId': session.consumerId}));
  return (response.data as List).map((e) => MeterTestingRow.fromJson(e as Map<String, dynamic>)).toList();
});

const _reasons = {
  'SuspectedIncorrectReading': 'Suspected incorrect reading',
  'HighConsumptionConcern': 'High consumption concern',
  'MeterDisplayIssue': 'Meter display issue',
  'AccuracyConcern': 'Accuracy concern',
  'IntermittentMeterIssue': 'Intermittent meter issue',
  'CommunicationConcern': 'Communication concern',
  'Other': 'Other',
};

Color _testingStatusColor(String status) {
  switch (status) {
    case 'Completed':
      return AppColors.success;
    case 'Rejected':
    case 'Cancelled':
      return AppColors.critical;
    case 'Scheduled':
      return AppColors.accent;
    default:
      return AppColors.warning;
  }
}

/// Meter Testing — its own dedicated module per the reference spec (not folded into Complaints).
/// Request a test, then track it through Submitted -> Scheduled -> Completed/Rejected/Cancelled.
/// Technician assignment/appointment detail and a downloadable test certificate from the full
/// spec are not implemented (this project's MeterTestingRequest doesn't model them).
class MeterTestingScreen extends ConsumerWidget {
  const MeterTestingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(meterTestingListProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Meter Testing')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const _RequestMeterTestScreen()));
          if (created == true) ref.invalidate(meterTestingListProvider);
        },
        icon: const Icon(Icons.add),
        label: const Text('Request Test'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(meterTestingListProvider),
        child: requestsAsync.when(
          data: (rows) => rows.isEmpty
              ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No meter testing requests yet.'))])
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: rows.length,
                  itemBuilder: (context, i) {
                    final r = rows[i];
                    return Card(
                      child: ListTile(
                        title: Text(r.requestNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text(
                            '${_reasons[r.reason] ?? r.reason}\nRaised ${DateFormat.yMMMd().format(r.createdAtUtc.toLocal())}'
                            '${r.testResult != null ? '\nResult: ${r.testResult} (${r.accuracyResult})' : ''}'),
                        isThreeLine: true,
                        trailing: StatusPill(text: r.status, color: _testingStatusColor(r.status)),
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

class _RequestMeterTestScreen extends ConsumerStatefulWidget {
  const _RequestMeterTestScreen();

  @override
  ConsumerState<_RequestMeterTestScreen> createState() => _RequestMeterTestScreenState();
}

class _RequestMeterTestScreenState extends ConsumerState<_RequestMeterTestScreen> {
  String _reason = _reasons.keys.first;
  final _remarksController = TextEditingController();
  bool _submitting = false;
  String? _error;

  Future<void> _submit() async {
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final session = ref.read(sessionProvider)!;
      final summary = await ref.read(summaryProvider.future);
      if (summary.meterId == null) {
        setState(() => _error = 'No meter is currently assigned to this connection.');
        return;
      }
      await apiCall(() => client.dio.post('/api/v1/meter-testing', data: {
            'customerId': session.consumerId,
            'meterId': summary.meterId,
            'reason': _reason,
            'consumerRemarks': _remarksController.text.trim().isEmpty ? null : _remarksController.text.trim(),
          }));
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Request Meter Test')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<String>(
              initialValue: _reason,
              decoration: const InputDecoration(labelText: 'Reason'),
              items: _reasons.entries.map((e) => DropdownMenuItem(value: e.key, child: Text(e.value))).toList(),
              onChanged: (v) => setState(() => _reason = v!),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _remarksController,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Remarks (optional)'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting ? const CircularProgressIndicator(strokeWidth: 2) : const Text('SUBMIT REQUEST'),
            ),
          ],
        ),
      ),
    );
  }
}
