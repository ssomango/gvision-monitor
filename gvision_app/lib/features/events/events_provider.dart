import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../core/api/events_api.dart';
import '../../core/models/event.dart';
import '../../core/ws/ws_client.dart';

class EventsProvider extends ChangeNotifier {
  final WsClient _ws;

  final List<GvisionEvent> _events = [];
  bool loading = true;
  int? _activeLogType; // null = 전체

  StreamSubscription<WsMessage>? _sub;

  EventsProvider(this._ws) {
    _init();
  }

  List<GvisionEvent> get events => List.unmodifiable(_events);

  Future<void> _init() async {
    await _fetch();

    _sub = _ws.stream.listen((msg) {
      if (msg.type == WsMessageType.newEvent ||
          msg.type == WsMessageType.alert) {
        final event = GvisionEvent.fromJson(msg.data);
        // 현재 필터와 맞으면 맨 앞에 삽입
        if (_activeLogType == null || event.logType == _activeLogType) {
          _events.insert(0, event);
          if (_events.length > 200) _events.removeLast();
          notifyListeners();
        }
      }
    });
  }

  Future<void> _fetch({int? logType}) async {
    loading = true;
    notifyListeners();
    try {
      final events = await EventsApi.fetchRecent(limit: 100, logType: logType);
      _events
        ..clear()
        ..addAll(events);
    } catch (_) {}
    loading = false;
    notifyListeners();
  }

  Future<void> filterBy(int? logType) async {
    _activeLogType = logType;
    await _fetch(logType: logType);
  }

  Future<void> refresh() => _fetch(logType: _activeLogType);

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}
