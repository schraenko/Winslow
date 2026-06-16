// lib/main.dart

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'core/api/api_client.dart';
import 'core/plugin_system/plugin_registry.dart';
import 'features/requirements/data/requirements_repository.dart';
import 'features/requirements/presentation/bloc/requirements_notifier.dart';
import 'plugins/requirements_plugin.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // ── Plugins registrieren ────────────────────────────────────────────────────
  final registry = PluginRegistry.instance;
  registry.register(RequirementsPlugin());
  // registry.register(IdeationPlugin());
  // registry.register(ProjectManagementPlugin());
  await registry.initializeAll();

  runApp(
    ProviderScope(
      overrides: [
        requirementsRepositoryProvider.overrideWithValue(
          RequirementsRepository(
            dio: ApiClient(
              baseUrl: const String.fromEnvironment(
                'API_BASE_URL',
                defaultValue: 'http://localhost:5000',
              ),
            ).dio,
          ),
        ),
      ],
      child: WinslowApp(registry: registry),
    ),
  );
}

class WinslowApp extends StatelessWidget {
  const WinslowApp({super.key, required this.registry});
  final PluginRegistry registry;

  @override
  Widget build(BuildContext context) {
    final router = GoRouter(
      initialLocation: '/projects/00000000-0000-0000-0000-000000000001/requirements',
      routes: [
        ShellRoute(
          builder: (context, state, child) => _AppShell(
            registry: registry,
            child: child,
          ),
          routes: registry.allRoutes,
        ),
      ],
    );

    return MaterialApp.router(
      title: 'Winslow',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF5C6BC0)),
        useMaterial3: true,
      ),
      routerConfig: router,
    );
  }
}

// ── App Shell mit Navigation ──────────────────────────────────────────────────

class _AppShell extends StatelessWidget {
  const _AppShell({required this.registry, required this.child});
  final PluginRegistry registry;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Row(
        children: [
          NavigationRail(
            destinations: registry.all.map((p) => NavigationRailDestination(
                  icon: Icon(Icons.extension),
                  label: Text(p.displayName),
                )).toList(),
            selectedIndex: 0,
            onDestinationSelected: (i) {
              final plugin = registry.all[i];
              context.go('${plugin.routePrefix}/00000000-0000-0000-0000-000000000001');
            },
          ),
          const VerticalDivider(width: 1),
          Expanded(child: child),
        ],
      ),
    );
  }
}
