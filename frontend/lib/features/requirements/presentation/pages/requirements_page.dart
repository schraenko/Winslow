// lib/features/requirements/presentation/pages/requirements_page.dart

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../bloc/requirements_notifier.dart';
import '../../domain/requirement.dart';
import '../widgets/requirement_card.dart';
import '../widgets/status_filter_bar.dart';

class RequirementsPage extends ConsumerStatefulWidget {
  const RequirementsPage({super.key, required this.projectId});
  final String projectId;

  @override
  ConsumerState<RequirementsPage> createState() => _RequirementsPageState();
}

class _RequirementsPageState extends ConsumerState<RequirementsPage> {
  RequirementStatus? _statusFilter;

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(requirementsNotifierProvider(widget.projectId));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Anforderungen'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Neue Anforderung',
            onPressed: () => _showCreateSheet(context),
          ),
        ],
      ),
      body: Column(
        children: [
          StatusFilterBar(
            selected: _statusFilter,
            onChanged: (s) => setState(() => _statusFilter = s),
          ),
          Expanded(
            child: switch (state) {
              RequirementsInitial() || RequirementsLoading() =>
                const Center(child: CircularProgressIndicator()),
              RequirementsError(:final message) =>
                Center(child: Text('Fehler: $message')),
              RequirementsLoaded(:final items) => _buildList(items),
            },
          ),
        ],
      ),
    );
  }

  Widget _buildList(List<Requirement> items) {
    final filtered = _statusFilter == null
        ? items
        : items.where((r) => r.status == _statusFilter).toList();

    if (filtered.isEmpty) {
      return const Center(
        child: Text('Keine Anforderungen gefunden.', style: TextStyle(color: Colors.grey)),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: filtered.length,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (_, i) => RequirementCard(
        requirement: filtered[i],
        onStatusChange: (newStatus) => ref
            .read(requirementsNotifierProvider(widget.projectId).notifier)
            .transitionStatus(filtered[i].id, newStatus),
      ),
    );
  }

  void _showCreateSheet(BuildContext context) {
    // TODO: CreateRequirementSheet
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Anforderung erstellen – coming soon')),
    );
  }
}
