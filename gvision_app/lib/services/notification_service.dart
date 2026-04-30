import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

class NotificationService {
  static final _plugin = FlutterLocalNotificationsPlugin();
  static int _id = 0;

  static void Function(int eventId)? onAlertTapped;

  static Future<void> init() async {
    const android = AndroidInitializationSettings('@mipmap/ic_launcher');
    const settings = InitializationSettings(android: android);

    await _plugin.initialize(
      settings,
      onDidReceiveNotificationResponse: (details) {
        final payload = details.payload;
        if (payload != null) {
          final eventId = int.tryParse(payload);
          if (eventId != null) onAlertTapped?.call(eventId);
        }
      },
    );
  }

  static Future<void> showAlert({
    required String title,
    required String body,
    int? eventId,
  }) async {
    // Android가 아닌 플랫폼(Windows 데스크탑 등)은 콘솔 출력으로 대체
    if (!defaultTargetPlatform.isAndroid) {
      debugPrint('[Notification] $title: $body');
      return;
    }

    const androidDetails = AndroidNotificationDetails(
      'gvision_alerts',
      'GVision 알림',
      channelDescription: '장비 이상 및 검사 알림',
      importance: Importance.high,
      priority: Priority.high,
    );
    const details = NotificationDetails(android: androidDetails);

    await _plugin.show(
      _id++,
      title,
      body,
      details,
      payload: eventId?.toString(),
    );
  }
}

extension on TargetPlatform {
  bool get isAndroid => this == TargetPlatform.android;
}
