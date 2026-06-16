// lib/core/plugin_system/plugin_registry.dart

import 'package:go_router/go_router.dart';

import 'plugin.dart';

/// Zentrale Plugin-Registry der Suite.
/// Plugins werden beim App-Start registriert und verwaltet.
class PluginRegistry {
  PluginRegistry._();
  static final PluginRegistry instance = PluginRegistry._();

  final Map<String, SuitePlugin> _plugins = {};

  /// Plugin registrieren
  void register(SuitePlugin plugin) {
    if (_plugins.containsKey(plugin.id)) {
      throw StateError('Plugin "${plugin.id}" ist bereits registriert.');
    }
    _plugins[plugin.id] = plugin;
  }

  /// Alle registrierten Plugins
  List<SuitePlugin> get all => _plugins.values.toList();

  /// Plugin nach ID abrufen
  SuitePlugin? find(String id) => _plugins[id];

  /// Alle Routen aller Plugins zusammenführen
  List<RouteBase> get allRoutes =>
      _plugins.values.expand((p) => p.routes).toList();

  /// Alle Plugins initialisieren
  Future<void> initializeAll() async {
    for (final plugin in _plugins.values) {
      await plugin.initialize();
    }
  }
}
