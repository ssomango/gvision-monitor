import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../core/api/inspections_api.dart';
import '../../core/ws/ws_client.dart';

class InspectionProvider extends ChangeNotifier {
  final WsClient _ws;

  List<dynamic> _series = [];
  List<Map<String, dynamic>> yieldSeries = []; // [{minute, total, pass, yield}]
  bool loading = true;

  // 오늘 집계
  int total = 0, good = 0, noDevice = 0, reject = 0, xout = 0;
  // 타입별 (1=MARK, 3=BGA, 5=2DCODE)
  int markTotal = 0, markNg = 0;
  int bgaTotal = 0, bgaNg = 0;
  int codeTotal = 0, codeNg = 0;

  StreamSubscription<WsMessage>? _sub;
  Timer? _pollTimer;

  InspectionProvider(this._ws) {
    _init();
  }

  Future<void> _init() async {
    await _fetch();

    // 검사 결과(logType=2) 새 이벤트 수신 시 즉시 갱신
    _sub = _ws.stream.listen((msg) {
      if (msg.type == WsMessageType.newEvent ||
          msg.type == WsMessageType.alert) {
        final logType = msg.data['LogType'] as int?;
        if (logType == 2) _fetch(); // InspectionLogs
      }
    });

    // 30초마다 폴링 (WebSocket이 놓친 변경 보완)
    _pollTimer = Timer.periodic(const Duration(seconds: 30), (_) => _fetch());
  }

  Future<void> _fetch() async {
    try {
      final results = await Future.wait([
        InspectionsApi.fetchSeries(),
        InspectionsApi.fetchYieldSeries(),
      ]);
      _series = results[0];
      yieldSeries = (results[1] as List).cast<Map<String, dynamic>>();
      _aggregate();
    } catch (e) {
      debugPrint('[InspectionProvider] fetch 에러: $e');
    }
    loading = false;
    notifyListeners();
  }

  void _aggregate() {
    total = _series.length;
    good = noDevice = reject = xout = 0;
    markTotal = markNg = bgaTotal = bgaNg = codeTotal = codeNg = 0;

    for (final r in _series) {
      final item = r['Item'] as String? ?? '';
      final type = r['InspectionType'] as int? ?? 0;

      if (item == 'PASS') {
        good++;
      } else if (item.contains('NoDevice')) {
        noDevice++;
      } else if (item.contains('XOut')) {
        xout++;
      } else {
        reject++;
      }

      switch (type) {
        case 1:
          markTotal++;
          if (item != 'PASS') markNg++;
        case 3:
          bgaTotal++;
          if (item != 'PASS') bgaNg++;
        case 5:
          codeTotal++;
          if (item != 'PASS') codeNg++;
      }
    }
  }

  double get yieldRate {
    final denom = total - noDevice - xout;
    return denom > 0 ? good / denom * 100 : 0.0;
  }

  Future<void> refresh() => _fetch();

  @override
  void dispose() {
    _sub?.cancel();
    _pollTimer?.cancel();
    super.dispose();
  }
}
