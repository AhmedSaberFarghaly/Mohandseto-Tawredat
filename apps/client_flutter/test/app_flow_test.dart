import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mohandseto_client/main.dart';

void main() {
  testWidgets('splash transitions to Arabic login screen', (tester) async {
    tester.view.physicalSize = const Size(1080, 1920);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const ProviderScope(child: MohandsetoApp()));
    expect(find.text('مهندسيتو توريدات'), findsOneWidget);

    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();

    expect(find.text('مرحبًا بعودتك'), findsOneWidget);
    expect(find.text('إرسال رمز التحقق'), findsOneWidget);
    expect(find.text('المتابعة باستخدام Google'), findsOneWidget);
    final loginContext = tester.element(find.text('مرحبًا بعودتك'));
    expect(MediaQuery.textScalerOf(loginContext).scale(10), closeTo(14, 0.01));
    expect(
      Theme.of(loginContext).textTheme.bodyMedium?.fontFamily,
      'IBMPlexSansArabic',
    );
  });
}
