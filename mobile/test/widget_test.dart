import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mdms_consumer/main.dart';

void main() {
  testWidgets('App shows the login screen when signed out', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: MdmsConsumerApp()));
    await tester.pump();

    expect(find.text('MDMS Consumer'), findsOneWidget);
    expect(find.widgetWithText(TextField, 'Consumer / Account Number'), findsOneWidget);
  });
}
