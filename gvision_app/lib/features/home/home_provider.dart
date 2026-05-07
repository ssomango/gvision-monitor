import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../core/models/device_status.dart';
import '../../core/models/event.dart';
import '../../core/api/events_api.dart';
import '../../core/ws/ws_client.dart';
import '../../services/notification_service.dart';

class HomeProvider extends ChangeNotifier {
  final WsClient _ws;

  DeviceStatus _status = DeviceStatus.offline();
  final List<GvisionEvent> _recentEvents = [];
  bool _loading = true;
  StreamSubscription<WsMessage>? _sub;

  DeviceStatus get status => _status;
  List<GvisionEvent> get recentEvents => List.unmodifiable(_recentEvents);
  bool get loading => _loading;

  HomeProvider(this._ws) {
    _init();
  }

  Future<void> _init() async {
    await _loadEvents();
    _sub = _ws.stream.listen(_onMessage);
    _loading = false;
    notifyListeners();
  }

  Future<void> _loadEvents() async {
    try {
      final events = await EventsApi.fetchRecent(limit: 30);
      _recentEvents
        ..clear()
        ..addAll(events);
    } catch (_) {}
  }

  void _onMessage(WsMessage msg) {
    switch (msg.type) {
      case WsMessageType.status:
        _status = DeviceStatus.fromJson(msg.data);
        notifyListeners();

      case WsMessageType.gvisionOffline:
        _status = DeviceStatus.offline();
        notifyListeners();

      case WsMessageType.alert:
      case WsMessageType.newEvent:
        final event = GvisionEvent.fromJson(msg.data);
        _recentEvents.insert(0, event);
        if (_recentEvents.length > 100) _recentEvents.removeLast();
        notifyListeners();

        // ALERT 타입이거나, NEW_EVENT라도 isAlert(LOT 포함)이면 알림 발송
        if (msg.type == WsMessageType.alert || event.isAlert) {
          NotificationService.showAlert(
            title: '[${event.logTypeLabel}] 이벤트 발생',
            body: event.description,
            eventId: event.id,
          );
        }

      case WsMessageType.unknown:
        break;
    }
  }

  Future<void> refresh() async {
    await _loadEvents();
    notifyListeners();
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}
