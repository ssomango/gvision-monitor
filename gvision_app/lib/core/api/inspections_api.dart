import 'api_client.dart';

class InspectionsApi {
  /// 오늘 수율 시계열 (1분 집계)
  static Future<List<dynamic>> fetchYieldSeries() async {
    final data = await ApiClient.get('/api/inspections/yield');
    return data['series'] as List<dynamic>? ?? [];
  }

  /// 불량 유형별 건수
  static Future<List<dynamic>> fetchErrors({String? lotId}) async {
    final q = lotId != null ? '?lotId=$lotId' : '';
    final data = await ApiClient.get('/api/inspections/errors$q');
    return data['errors'] as List<dynamic>? ?? [];
  }

  /// 원시 검사 결과 (오늘)
  static Future<List<dynamic>> fetchSeries({String? from, String? to, String? lotId}) async {
    final params = <String>[];
    if (from != null) params.add('from=$from');
    if (to != null) params.add('to=$to');
    if (lotId != null) params.add('lotId=$lotId');
    final q = params.isEmpty ? '' : '?${params.join('&')}';
    final data = await ApiClient.get('/api/inspections/series$q');
    return data['results'] as List<dynamic>? ?? [];
  }
}
