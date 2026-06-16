// lib/plugins/requirements_plugin.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../core/plugin_system/plugin.dart';
import '../features/requirements/presentation/pages/requirements_page.dart';

class RequirementsPlugin implements SuitePlugin {
  @override
  String get id => 'requirements';

  @override
  String get displayName => 'Anforderungen';

  @override
  String get iconAsset => 'checklist';

  @override
  String get routePrefix => '/requirements';

  @override
  List<RouteBase> get routes => [
        GoRoute(
          path: '/projects/:projectId/requirements',
          builder: (context, state) => RequirementsPage(
            projectId: state.pathParameters['projectId']!,
          ),
        ),
      ];

  @override
  Future<void> initialize() async {
    // Dependency-Injection, lokale DB-Migrationen etc.
    debugPrint('RequirementsPlugin initialisiert.');
  }
}
