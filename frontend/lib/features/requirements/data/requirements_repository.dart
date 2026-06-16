// lib/features/requirements/data/requirements_repository.dart

import 'package:dio/dio.dart';
import '../domain/requirement.dart';

class RequirementsRepository {
  RequirementsRepository({required Dio dio}) : _dio = dio;
  final Dio _dio;

  Future<List<Requirement>> getByProject(String projectId) async {
    final response = await _dio.get<List>('/projects/$projectId/requirements');
    return (response.data ?? [])
        .cast<Map<String, dynamic>>()
        .map(Requirement.fromJson)
        .toList();
  }

  Future<Requirement> getById(String id) async {
    final response = await _dio.get<Map<String, dynamic>>('/requirements/$id');
    return Requirement.fromJson(response.data!);
  }

  Future<String> create({
    required String projectId,
    required String title,
    required String description,
    required RequirementPriority priority,
    required RequirementKind kind,
    required List<String> acceptanceCriteria,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/requirements',
      data: {
        'projectId'          : projectId,
        'title'              : title,
        'description'        : description,
        'priority'           : priority.name,
        'kind'               : kind.name,
        'acceptanceCriteria' : acceptanceCriteria,
      },
    );
    return response.data!['id'] as String;
  }

  Future<void> transitionStatus(String id, RequirementStatus newStatus) async {
    await _dio.patch<void>(
      '/requirements/$id/status',
      data: {'newStatus': newStatus.name},
    );
  }
}
