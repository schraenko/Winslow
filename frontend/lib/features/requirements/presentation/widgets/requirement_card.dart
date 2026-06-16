// lib/features/requirements/presentation/widgets/requirement_card.dart

import 'package:flutter/material.dart';
import '../../domain/requirement.dart';

class RequirementCard extends StatelessWidget {
  const RequirementCard({
    super.key,
    required this.requirement,
    required this.onStatusChange,
  });

  final Requirement requirement;
  final ValueChanged<RequirementStatus> onStatusChange;

  Color _statusColor(RequirementStatus s) => switch (s) {
        RequirementStatus.draft        => Colors.grey,
        RequirementStatus.underReview  => Colors.orange,
        RequirementStatus.approved     => Colors.green,
        RequirementStatus.rejected     => Colors.red,
        RequirementStatus.implemented  => Colors.blue,
      };

  Color _priorityColor(RequirementPriority p) => switch (p) {
        RequirementPriority.mustHave   => Colors.red.shade700,
        RequirementPriority.shouldHave => Colors.orange.shade700,
        RequirementPriority.couldHave  => Colors.blue.shade700,
        RequirementPriority.wontHave   => Colors.grey,
      };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    requirement.title,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                _StatusChip(
                  status: requirement.status,
                  color: _statusColor(requirement.status),
                  onStatusChange: onStatusChange,
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              requirement.description,
              style: theme.textTheme.bodyMedium?.copyWith(color: Colors.grey[600]),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                _Chip(
                  label: requirement.priority.label,
                  color: _priorityColor(requirement.priority),
                ),
                const SizedBox(width: 8),
                _Chip(
                  label: requirement.kind.label,
                  color: Colors.purple.shade700,
                ),
                const Spacer(),
                Text(
                  '${requirement.acceptanceCriteria.length} Kriterien',
                  style: theme.textTheme.bodySmall?.copyWith(color: Colors.grey),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({
    required this.status,
    required this.color,
    required this.onStatusChange,
  });
  final RequirementStatus status;
  final Color color;
  final ValueChanged<RequirementStatus> onStatusChange;

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<RequirementStatus>(
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: color.withOpacity(0.12),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: color.withOpacity(0.4)),
        ),
        child: Text(status.label, style: TextStyle(color: color, fontSize: 12)),
      ),
      itemBuilder: (_) => RequirementStatus.values
          .where((s) => s != status)
          .map((s) => PopupMenuItem(value: s, child: Text(s.label)))
          .toList(),
      onSelected: onStatusChange,
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, required this.color});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(label, style: TextStyle(color: color, fontSize: 11)),
    );
  }
}
