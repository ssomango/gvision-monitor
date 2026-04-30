import 'api_client.dart';

class InspectionsApi {
  /// KST 'YYYY-MM-DD HH:MM:SS' 형식 문자열 반환 (DB 포맷과 일치)
  static String _kstStr(DateTime dt) {
    final kst = dt.toLocal();
    return '${kst.year.toString().padLeft(4, '0')}-'
        '${kst.month.toString().padLeft(2, '0')}-'
        '${kst.day.toString().padLeft(2, '0')} '
        '${kst.hour.toString().padLeft(2, '0')}:'
        '${kst.minute.toString().padLeft(2, '0')}:'
        '${kst.second.toString().padLeft(2, '0')}';
  }

  /// 오늘 00:00 KST ~ 지금 범위
  static (String, String) _todayRange() {
    final now = DateTime.now();
    final todayStart = DateTime(now.year, now.month, now.day);
    return (_kstStr(todayStart), _kstStr(now));
  }

  /// 수율 시계열 (오늘 전체, 1분 집계)
  static Future<List<Map<String, dynamic>>> fetchYieldSeries() async {
    final (from, to) = _todayRange();
    final data = await ApiClient.get(
        '/api/inspections/yield?from=${Uri.encodeComponent(from)}&to=${Uri.encodeComponent(to)}');
    final list = data['data'] as List<dynamic>? ?? [];
    return list.cast<Map<String, dynamic>>();
  }

  /// 오늘 원시 검사 결과 전체
  static Future<List<dynamic>> fetchSeries() async {
    final (from, to) = _todayRange();
    final data = await ApiClient.get(
        '/api/inspections/series?from=${Uri.encodeComponent(from)}&to=${Uri.encodeComponent(to)}');
    return data['data'] as List<dynamic>? ?? [];
  }

  /// 불량 유형별 건수 (lotId 필수)
  static Future<List<dynamic>> fetchErrors({required int lotId}) async {
    final data = await ApiClient.get('/api/inspections/errors?lotId=$lotId');
    return data['data'] as List<dynamic>? ?? [];
  }
}
