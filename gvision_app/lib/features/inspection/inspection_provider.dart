import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../core/api/inspections_api.dart';
import '../../core/ws/ws_client.dart';

enum RangeType { hour1, hour6, today }

enum ShotResultFilter {
  all,
  failOnly,
  yieldDrop,
}

class InspectionProvider extends ChangeNotifier {
  final WsClient _ws;

  List<dynamic> _series = [];
  List<Map<String, dynamic>> yieldSeries = []; // [{minute, total, pass, yield}]
  bool loading = true;

  List<dynamic> get series => List.unmodifiable(_series);

  RangeType _rangeType = RangeType.today;
  RangeType get rangeType => _rangeType;

  double dropThreshold = 5.0;

  void setDropThreshold(double value) {
    dropThreshold = value;
    notifyListeners();
  }

  double abnormalYieldThreshold = 90.0;

  void setAbnormalYieldThreshold(double value) {
    abnormalYieldThreshold = value;
    notifyListeners();
  }

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
      final now = DateTime.now();

      DateTime from;

      switch (_rangeType) {
        case RangeType.hour1:
          from = now.subtract(const Duration(hours: 1));
          break;
        case RangeType.hour6:
          from = now.subtract(const Duration(hours: 6));
          break;
        case RangeType.today:
        default:
          from = DateTime(now.year, now.month, now.day);
          break;
      }

      final results = await Future.wait([
        InspectionsApi.fetchSeries(from: from, to: now),
        InspectionsApi.fetchYieldSeries(from: from, to: now),
      ]);

      _series = (results[0] as List).cast<dynamic>();
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

  void setRange(RangeType type) {
    _rangeType = type;
    loading = true;
    notifyListeners();
    _fetch();
  }

  Future<void> refresh() => _fetch();

  @override
  void dispose() {
    _sub?.cancel();
    _pollTimer?.cancel();
    super.dispose();
  }

  //shot filter
  ShotResultFilter shotFilter = ShotResultFilter.all;
  String selectedInspectionType = 'ALL'; // ALL, MARK, BGA, 2D
  double yieldThreshold = 90.0;

  void setShotFilter(ShotResultFilter filter) {
    shotFilter = filter;
    notifyListeners();
  }

  void setInspectionType(String type) {
    selectedInspectionType = type;
    notifyListeners();
  }

  void setYieldThreshold(double value) {
    yieldThreshold = value;
    notifyListeners();
  }

  List<dynamic> get filteredShots {
    return _series.where((shot) {
      final item = shot['Item'] as String? ?? '';
      final type = shot['InspectionType'] as int? ?? 0;

      final isFail = item != 'PASS';

      final typeName = switch (type) {
        1 => 'MARK',
        3 => 'BGA',
        5 => '2D',
        _ => 'UNKNOWN',
      };

      final matchType =
          selectedInspectionType == 'ALL' ||
              selectedInspectionType == typeName;

      final matchResult = switch (shotFilter) {
        ShotResultFilter.all => true,
        ShotResultFilter.failOnly => isFail,
        ShotResultFilter.yieldDrop => isFail,
      };

      return matchType && matchResult;
    }).toList();
  }
}
