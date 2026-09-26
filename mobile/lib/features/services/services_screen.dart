import 'package:flutter/material.dart';
import '../recharge/recharge_screen.dart';
import '../bills/bills_screen.dart';
import '../complaints/complaints_screen.dart';

class _ServiceItem {
  final String label;
  final String subtitle;
  final IconData icon;
  final WidgetBuilder builder;
  const _ServiceItem(this.label, this.subtitle, this.icon, this.builder);
}

/// Services hub — Recharge, Bills, Transactions (inside Recharge & Wallet), Complaints. Service
/// Requests and Meter Testing from the reference spec are not implemented: neither has a backend
/// endpoint in this project yet (ComplaintsController only models Complaint, not a separate
/// ServiceRequest/MeterTestingRequest entity), so they are left off rather than shown as dead
/// buttons.
class ServicesScreen extends StatelessWidget {
  const ServicesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final items = [
      _ServiceItem('Recharge & Wallet', 'Balance, recharge, transaction history', Icons.account_balance_wallet_outlined, (_) => const RechargeScreen()),
      _ServiceItem('Bills & Statements', 'Billing profile history', Icons.receipt_long_outlined, (_) => const BillsScreen()),
      _ServiceItem('Complaints', 'Raise and track complaints', Icons.support_agent_outlined, (_) => const ComplaintsScreen()),
    ];

    return Scaffold(
      appBar: AppBar(title: const Text('Services')),
      body: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, i) {
          final item = items[i];
          return Card(
            child: ListTile(
              contentPadding: const EdgeInsets.all(12),
              leading: CircleAvatar(backgroundColor: Theme.of(context).colorScheme.primaryContainer, child: Icon(item.icon)),
              title: Text(item.label, style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: Text(item.subtitle),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: item.builder)),
            ),
          );
        },
      ),
    );
  }
}
