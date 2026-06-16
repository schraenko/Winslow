// lib/features/requirements/presentation/bloc/requirements_notifier.dart

import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/requirements_repository.dart';
import '../../domain/requirement.dart';

// ── State ─────────────────────────────────────────────────────────────────────

sealed class RequirementsState {
  const RequirementsState();
}

class RequirementsInitial extends RequirementsState {
  const RequirementsInitial();
}

class RequirementsLoading extends RequirementsState {
  const RequirementsLoading();
}

class RequirementsLoaded extends RequirementsState {
  const RequirementsLoaded(this.items);
  final List<Requirement> items;
}

class RequirementsError extends RequirementsState {
  const RequirementsError(this.message);
  final String message;
}

// ── Notifier ──────────────────────────────────────────────────────────────────

class RequirementsNotifier extends StateNotifier<RequirementsState> {
  RequirementsNotifier(this._repo) : super(const RequirementsInitial());

  final RequirementsRepository _repo;

  Future<void> loadForProject(String projectId) async {
    state = const RequirementsLoading();
    try {
      final items = await _repo.getByProject(projectId);
      state = RequirementsLoaded(items);
    } catch (e) {
      state = RequirementsError(e.toString());
    }
  }

  Future<void> transitionStatus(String id, RequirementStatus newStatus) async {
    await _repo.transitionStatus(id, newStatus);
    // Optimistisches Update
    if (state is RequirementsLoaded) {
      final updated = (state as RequirementsLoaded).items.map((r) {
        return r.id == id ? r.copyWith(status: newStatus) : r;
      }).toList();
      state = RequirementsLoaded(updated);
    }
  }
}

// ── Provider ──────────────────────────────────────────────────────────────────

final requirementsNotifierProvider = StateNotifierProvider.family<
    RequirementsNotifier, RequirementsState, String>(
  (ref, projectId) {
    final repo = ref.watch(requirementsRepositoryProvider);
    return RequirementsNotifier(repo)..loadForProject(projectId);
  },
);

// Muss in main.dart mit konkretem Dio-Client überschrieben werden
final requirementsRepositoryProvider = Provider<RequirementsRepository>(
  (ref) => throw UnimplementedError('requirementsRepositoryProvider nicht initialisiert'),
);
