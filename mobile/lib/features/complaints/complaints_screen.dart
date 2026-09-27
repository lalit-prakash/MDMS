import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

class ComplaintRow {
  final String id;
  final String description;
  final String status;
  final DateTime createdAtUtc;
  final DateTime slaDueUtc;
  final String? resolutionNote;

  ComplaintRow.fromJson(Map<String, dynamic> j)
      : id = j['id'] as String,
        description = j['description'] as String,
        status = j['status'] as String,
        createdAtUtc = DateTime.parse(j['createdAtUtc'] as String),
        slaDueUtc = DateTime.parse(j['slaDueUtc'] as String),
        resolutionNote = j['resolutionNote'] as String?;
}

final complaintsProvider = FutureProvider.autoDispose<List<ComplaintRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/complaints', queryParameters: {'customerId': session.consumerId}));
  return (response.data as List).map((e) => ComplaintRow.fromJson(e as Map<String, dynamic>)).toList();
});

Color _statusColor(String status) {
  switch (status) {
    case 'Open':
      return Colors.orange;
    case 'Assigned':
    case 'InProgress':
      return Colors.blue;
    case 'Resolved':
    case 'Closed':
      return Colors.green;
    default:
      return Colors.grey;
  }
}

/// Complaint raise + tracking, backed by ComplaintsController. WFM/technician-assignment detail
/// and feedback/rating from the full spec are not implemented — Complaint itself carries no such
/// fields yet.
class ComplaintsScreen extends ConsumerWidget {
  const ComplaintsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final complaintsAsync = ref.watch(complaintsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Complaints')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          final created = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const _RaiseComplaintScreen()));
          if (created == true) ref.invalidate(complaintsProvider);
        },
        icon: const Icon(Icons.add),
        label: const Text('Raise Complaint'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(complaintsProvider),
        child: complaintsAsync.when(
          data: (rows) => rows.isEmpty
              ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No complaints raised yet.'))])
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: rows.length,
                  itemBuilder: (context, i) {
                    final c = rows[i];
                    return Card(
                      child: ListTile(
                        title: Text(c.description, maxLines: 2, overflow: TextOverflow.ellipsis),
                        subtitle: Text('Raised ${DateFormat.yMMMd().format(c.createdAtUtc.toLocal())} · SLA due ${DateFormat.yMMMd().format(c.slaDueUtc.toLocal())}'
                            '${c.resolutionNote != null ? '\nResolution: ${c.resolutionNote}' : ''}'),
                        isThreeLine: c.resolutionNote != null,
                        trailing: Chip(
                          label: Text(c.status, style: const TextStyle(color: Colors.white, fontSize: 12)),
                          backgroundColor: _statusColor(c.status),
                        ),
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

const _categories = [
  'Meter not working',
  'No power',
  'Low voltage',
  'Recharge not reflected',
  'Incorrect bill',
  'Payment deducted but recharge not received',
  'Other',
];

class _RaiseComplaintScreen extends ConsumerStatefulWidget {
  const _RaiseComplaintScreen();

  @override
  ConsumerState<_RaiseComplaintScreen> createState() => _RaiseComplaintScreenState();
}

class _RaiseComplaintScreenState extends ConsumerState<_RaiseComplaintScreen> {
  String _category = _categories.first;
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
      final description = '$_category: ${_descriptionController.text.trim()}';
      await apiCall(() => client.dio.post('/api/v1/complaints', data: {
            'customerId': session.consumerId,
            'meterId': null,
            'source': 'MobileApp',
            'description': description,
            'slaHours': 48,
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
      appBar: AppBar(title: const Text('Raise Complaint')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<String>(
              initialValue: _category,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Category', border: OutlineInputBorder()),
              items: _categories.map((c) => DropdownMenuItem(value: c, child: Text(c, overflow: TextOverflow.ellipsis))).toList(),
              onChanged: (v) => setState(() => _category = v!),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _descriptionController,
              maxLines: 4,
              decoration: const InputDecoration(labelText: 'Describe the issue', border: OutlineInputBorder()),
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
