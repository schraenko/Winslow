import 'package:go_router/go_router.dart';

/// Basis-Interface für alle Suite-Plugins.
/// Jedes Feature-Modul implementiert dieses Interface.
abstract class SuitePlugin {
  /// Eindeutiger Bezeichner des Plugins (z.B. 'requirements', 'ideation')
  String get id;

  /// Anzeigename in der Navigation
  String get displayName;

  /// Icon für die Navigation
  String get iconAsset;

  /// Route-Prefix des Plugins (z.B. '/requirements')
  String get routePrefix;

  /// GoRouter-Routen des Plugins
  List<RouteBase> get routes;

  /// Wird beim App-Start aufgerufen (Initialisierung, DI-Registrierung)
  Future<void> initialize();
}
