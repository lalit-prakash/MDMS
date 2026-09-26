import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class ServiceRequestRow {
  final String id;
  final String requestNumber;
  final String category;
  final String description;
  final String status;
  final DateTime createdAtUtc;

  ServiceRequestRow.fromJson(Map<String, dynamic> j)
      : id = j['id'] as String,
        requestNumber = j['requestNumber'] as String,
        category = j['category'] as String,
        description = j['description'] as String,
        status = j['status'] as String,
        createdAtUtc = DateTime.parse(j['createdAtUtc'] as String);
}

final serviceRequestsProvider = FutureProvider.autoDispose<List<ServiceRequestRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/service-requests', queryParameters: {'customerId': session.consumerId}));
  return (response.data as List).map((e) => ServiceRequestRow.fromJson(e as Map<String, dynamic>)).toList();
});

const _categories = {
  'MeterReplacement': 'Meter replacement',
  'MeterShifting': 'Meter shifting',
  'LoadEnhancement': 'Load enhancement',
  'LoadReduction': 'Load reduction',
  'NameCorrection': 'Name correction',
  'MobileNumberUpdate': 'Mobile number update',
  'EmailUpdate': 'Email update',
  'AddressCorrection': 'Address correction',
  'PrepaidPostpaidConversion': 'Prepaid/Postpaid conversion',
  'Other': 'Other',
};

Color _srStatusColor(String status) {
  switch (status) {
    case 'Completed':
      return AppColors.success;
    case 'Rejected':
    case 'Cancelled':
      return AppColors.critical;
    case 'InProgress':
      return AppColors.accent;
    default:
      return AppColors.warning;
  }
}

/// Service Requests — separate from Complaints per the reference spec (meter replacement/
/// shifting, load changes, name/contact/address correction, tariff conversion).
class ServiceRequestsScreen extends ConsumerWidget {
  const ServiceRequestsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(serviceRequestsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Service Requests')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const _RaiseServiceRequestScreen()));
          if (created == true) ref.invalidate(serviceRequestsProvider);
        },
        icon: const Icon(Icons.add),
        label: const Text('New Request'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(serviceRequestsProvider),
        child: requestsAsync.when(
          data: (rows) => rows.isEmpty
              ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No service requests yet.'))])
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: rows.length,
                  itemBuilder: (context, i) {
                    final r = rows[i];
                    return Card(
                      child: ListTile(
                        title: Text(_categories[r.category] ?? r.category, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text('${r.requestNumber}\n${r.description}\nRaised ${DateFormat.yMMMd().format(r.createdAtUtc.toLocal())}'),
                        isThreeLine: true,
                        trailing: StatusPill(text: r.status, color: _srStatusColor(r.status)),
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

class _RaiseServiceRequestScreen extends ConsumerStatefulWidget {
  const _RaiseServiceRequestScreen();

  @override
  ConsumerState<_RaiseServiceRequestScreen> createState() => _RaiseServiceRequestScreenState();
}

class _RaiseServiceRequestScreenState extends ConsumerState<_RaiseServiceRequestScreen> {
  String _category = _categories.keys.first;
  final _descriptionController = TextEditingController();
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
      await apiCall(() => client.dio.post('/api/v1/service-requests', data: {
            'customerId': session.consumerId,
            'category': _category,
            'description': _descriptionController.text.trim(),
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
      appBar: AppBar(title: const Text('New Service Request')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<String>(
              initialValue: _category,
              decoration: const InputDecoration(labelText: 'Category'),
              items: _categories.entries.map((e) => DropdownMenuItem(value: e.key, child: Text(e.value))).toList(),
              onChanged: (v) => setState(() => _category = v!),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _descriptionController,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Describe your request'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting ? const CircularProgressIndicator(strokeWidth: 2) : const Text('SUBMIT'),
            ),
          ],
        ),
      ),
    );
  }
}
