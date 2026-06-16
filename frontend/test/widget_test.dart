import 'package:flutter_test/flutter_test.dart';

import 'package:winslow/core/plugin_system/plugin_registry.dart';
import 'package:winslow/main.dart';

void main() {
  testWidgets('App renders', (WidgetTester tester) async {
    final registry = PluginRegistry.instance;
    await tester.pumpWidget(WinslowApp(registry: registry));
    expect(find.byType(WinslowApp), findsOneWidget);
  });
}
