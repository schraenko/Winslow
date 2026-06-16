// lib/features/requirements/presentation/widgets/status_filter_bar.dart

import 'package:flutter/material.dart';
import '../../domain/requirement.dart';

class StatusFilterBar extends StatelessWidget {
  const StatusFilterBar({
    super.key,
    required this.selected,
    required this.onChanged,
  });

  final RequirementStatus? selected;
  final ValueChanged<RequirementStatus?> onChanged;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          _FilterChip(
            label: 'Alle',
            selected: selected == null,
            onTap: () => onChanged(null),
          ),
          ...RequirementStatus.values.map((s) => Padding(
                padding: const EdgeInsets.only(left: 8),
                child: _FilterChip(
                  label: s.label,
                  selected: selected == s,
                  onTap: () => onChanged(s),
                ),
              )),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.primary;
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          color: selected ? color : color.withOpacity(0.08),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: selected ? Colors.white : color,
            fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
            fontSize: 13,
          ),
        ),
      ),
    );
  }
}
