import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:provider/provider.dart';
import 'inspection_provider.dart';
import '../../shared/widgets/stat_card.dart';

class InspectionScreen extends StatefulWidget {
  const InspectionScreen({super.key});

  @override
  State<InspectionScreen> createState() => _InspectionScreenState();
}

class _InspectionScreenState extends State<InspectionScreen> {
  int _tabIndex = 0;

  @override
  Widget build(BuildContext context) {
    final p = context.watch<InspectionProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Inspection 분석'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: p.refresh),
        ],
      ),
      body: p.loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
        children: [
          _tabSelector(),
          Expanded(
            child: RefreshIndicator(
              onRefresh: p.refresh,
              child: _tabIndex == 0
                  ? _overviewTab(p)
                  : _shotDetailTab(p),
            ),
          ),
        ],
      ),
    );
  }

  Widget _tabSelector() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: Row(
        children: [
          Expanded(child: _tabBtn('전체 분석', 0)),
          const SizedBox(width: 8),
          Expanded(child: _tabBtn('Shot별 상세', 1)),
        ],
      ),
    );
  }

  Widget _tabBtn(String label, int index) {
    final selected = _tabIndex == index;

    return ElevatedButton(
      style: ElevatedButton.styleFrom(
        backgroundColor: selected ? const Color(0xFF1976D2) : Colors.grey.shade800,
        foregroundColor: Colors.white,
      ),
      onPressed: () => setState(() => _tabIndex = index),
      child: Text(label),
    );
  }

  Widget _overviewTab(InspectionProvider p) {
    return ListView(
      children: [
        _rangeSelector(p),
        _sectionHeader(context, '오늘 검사 결과'),
        _summaryGrid(p),
        _sectionHeader(context, '수율 트렌드'),
        _dropThresholdSelector(p),
        _abnormalThresholdSelector(p),
        _yieldTrendChart(p),
        _sectionHeader(context, '검사 타입별 현황'),
        _typeTable(p),
        const SizedBox(height: 24),
      ],
    );
  }

  Widget _shotDetailTab(InspectionProvider p) {
    final data = p.series;

    if (data.isEmpty) {
      return ListView(
        children: [
          Padding(
            padding: EdgeInsets.all(24),
            child: Center(child: Text('Shot 데이터 없음')),
          ),
        ],
      );
    }

    return ListView(
      children: [
        _rangeSelector(p),
        // _sectionHeader(context, 'X/Y Offset 변화'),
        // _offsetTrendChart(p),
        _sectionHeader(context, '최근 이상 Shot 주변 컨텍스트'),
        _alertContextCard(p),
        _sectionHeader(context, '시간대별 검사 결과 분포'),
        _resultTrendChart(p),
        _sectionHeader(context, '검사 타입별 NG율 변화'),
        _typeNgTrendChart(p),
        _sectionHeader(context, 'Shot별 검사 상세'),
        ...data.map((shot) => _shotTile(shot)),
        const SizedBox(height: 24),
      ],
    );
  }

  Widget _shotTile(dynamic shot) {
    final item = shot['Item']?.toString() ?? '-';
    final time = shot['CreatedAt']?.toString() ??
        shot['createdAt']?.toString() ??
        shot['Time']?.toString() ??
        '-';
    final type = shot['InspectionType']?.toString() ?? '-';

    final isPass = item == 'PASS';
    final color = isPass ? const Color(0xFF43A047) : const Color(0xFFE53935);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: ListTile(
        leading: Icon(
          isPass ? Icons.check_circle : Icons.error,
          color: color,
        ),
        title: Text(
          item,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
        subtitle: Text('시간: $time / 검사 타입: $type'),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => _showShotDetail(context, shot),
      ),
    );
  }

  void _showShotDetail(BuildContext context, dynamic shot) {
    showDialog(
      context: context,
      builder: (_) {
        return AlertDialog(
          title: const Text('Shot 상세 정보'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                _detailRow('Item', shot['Item']),
                _detailRow('InspectionType', shot['InspectionType']),
                _detailRow(
                  'StartTime',
                  shot['StartTIme'] ?? shot['StartTime'] ?? shot['startTime'],
                ),
                _detailRow('EndTime', shot['EndTime'] ?? shot['endTime']),                _detailRow('LotId', shot['LotId'] ?? shot['lotId']),
                _detailRow('ShotId', shot['ShotId'] ?? shot['shotId'] ?? shot['Id']),
                _detailRow('X Offset', shot['XOffset'] ?? shot['xOffset'] ?? shot['X_Offset']),
                _detailRow('Y Offset', shot['YOffset'] ?? shot['yOffset'] ?? shot['Y_Offset']),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('닫기'),
            ),
          ],
        );
      },
    );
  }

  Widget _detailRow(String label, dynamic value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(
              label,
              style: const TextStyle(color: Colors.white54, fontSize: 12),
            ),
          ),
          Expanded(
            child: Text(
              '${value ?? '-'}',
              style: const TextStyle(fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }

  Widget _sectionHeader(BuildContext context, String title) => Padding(
    padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
    child: Text(
      title,
      style: const TextStyle(
        fontSize: 13,
        fontWeight: FontWeight.bold,
        color: Colors.white70,
      ),
    ),
  );

  Widget _summaryGrid(InspectionProvider p) {
    return GridView.count(
      crossAxisCount: 3,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      childAspectRatio: 1.6,
      padding: const EdgeInsets.symmetric(horizontal: 8),
      children: [
        StatCard(label: 'TOTAL', value: '${p.total}', icon: Icons.inventory_2),
        StatCard(
          label: 'GOOD',
          value: '${p.good}',
          icon: Icons.check_circle,
          valueColor: const Color(0xFF43A047),
        ),
        StatCard(
          label: 'NO DEVICE',
          value: '${p.noDevice}',
          icon: Icons.remove_circle,
          valueColor: Colors.grey,
        ),
        StatCard(
          label: 'REJECT',
          value: '${p.reject}',
          icon: Icons.cancel,
          valueColor: const Color(0xFFE53935),
        ),
        StatCard(
          label: 'X-OUT',
          value: '${p.xout}',
          icon: Icons.block,
          valueColor: Colors.orange,
        ),
        StatCard(
          label: 'YIELD',
          value: '${p.yieldRate.toStringAsFixed(1)}%',
          icon: Icons.percent,
          valueColor: p.yieldRate >= 99
              ? const Color(0xFF43A047)
              : p.yieldRate >= 95
              ? Colors.orange
              : const Color(0xFFE53935),
        ),
      ],
    );
  }

  Widget _typeTable(InspectionProvider p) {
    final rows = [
      ('MARK', p.markTotal, p.markNg),
      ('BGA', p.bgaTotal, p.bgaNg),
      ('2D CODE', p.codeTotal, p.codeNg),
    ];

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                Expanded(
                  child: Text('타입',
                      style: TextStyle(fontSize: 11, color: Colors.white54)),
                ),
                SizedBox(
                  width: 60,
                  child: Text('검사',
                      textAlign: TextAlign.right,
                      style: TextStyle(fontSize: 11, color: Colors.white54)),
                ),
                SizedBox(
                  width: 60,
                  child: Text('NG',
                      textAlign: TextAlign.right,
                      style: TextStyle(fontSize: 11, color: Colors.white54)),
                ),
                SizedBox(
                  width: 70,
                  child: Text('NG율',
                      textAlign: TextAlign.right,
                      style: TextStyle(fontSize: 11, color: Colors.white54)),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          ...rows.map((r) => _typeRow(r.$1, r.$2, r.$3)),
        ],
      ),
    );
  }

  Widget _typeRow(String label, int total, int ng) {
    final rate = total > 0 ? ng / total * 100 : 0.0;
    final color = rate == 0
        ? const Color(0xFF43A047)
        : rate < 1
        ? Colors.orange
        : const Color(0xFFE53935);

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      child: Row(
        children: [
          Expanded(
            child: Text(label,
                style: const TextStyle(fontWeight: FontWeight.w500)),
          ),
          SizedBox(
            width: 60,
            child: Text('$total',
                textAlign: TextAlign.right,
                style: const TextStyle(fontSize: 13)),
          ),
          SizedBox(
            width: 60,
            child: Text(
              '$ng',
              textAlign: TextAlign.right,
              style: TextStyle(
                fontSize: 13,
                color: ng > 0 ? const Color(0xFFE53935) : null,
              ),
            ),
          ),
          SizedBox(
            width: 70,
            child: Text(
              '${rate.toStringAsFixed(2)}%',
              textAlign: TextAlign.right,
              style: TextStyle(fontSize: 13, color: color),
            ),
          ),
        ],
      ),
    );
  }

  // 수율 그래프
  Widget _yieldTrendChart(InspectionProvider p) {
    final data = p.yieldSeries;


    if (data.isEmpty) {
      return const Padding(
        padding: EdgeInsets.all(16),
        child: Text('데이터 없음'),
      );
    }

    final spots = data.asMap().entries.map((e) {
      final i = e.key;
      final y = (e.value['yield'] as num).toDouble();
      return FlSpot(i.toDouble(), y);
    }).toList();

    final abnormalSpots = spots
        .where((s) => s.y < p.abnormalYieldThreshold)
        .toList();

    int? dropIndex;
    double biggestDrop = 0;

    for (int i = 1; i < spots.length; i++) {
      final diff = spots[i].y - spots[i - 1].y;

      if (diff < biggestDrop) {
        biggestDrop = diff;
        dropIndex = i;
      }
    }

    String? dropSummary;

    if (dropIndex != null && biggestDrop <= -p.dropThreshold) {
      final raw = data[dropIndex]['minute']?.toString() ?? '';
      final dt = DateTime.tryParse(raw);

      final timeText = dt == null
          ? '해당 구간'
          : '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';

      dropSummary =
      '$timeText 부근에서 수율이 ${biggestDrop.abs().toStringAsFixed(1)}%p 급락했습니다. 해당 시간대의 Shot 상세와 검사 타입별 NG율을 함께 확인하세요.';
    }  else {
      dropSummary =
      '선택한 기간에서는 ${p.dropThreshold.toStringAsFixed(0)}%p 이상 급격한 수율 하락 구간이 감지되지 않았습니다.';
    }

    final abnormalText = abnormalSpots.isNotEmpty
        ? ' 또한 ${p.abnormalYieldThreshold.toStringAsFixed(0)}% 미만 이상 구간이 ${abnormalSpots.length}개 감지되었습니다.'
        : ' ${p.abnormalYieldThreshold.toStringAsFixed(0)}% 미만 이상 구간은 감지되지 않았습니다.';

    dropSummary = '$dropSummary$abnormalText';




    String timeLabel(int index) {
      if (index < 0 || index >= data.length) return '';

      final raw = data[index]['minute']?.toString() ?? '';
      final dt = DateTime.tryParse(raw);

      if (dt == null) return '';

      final h = dt.hour.toString().padLeft(2, '0');
      final m = dt.minute.toString().padLeft(2, '0');
      return '$h:$m';
    }

    return _chartCard(
      title: '수율 트렌드',
      description: '선택한 기간 동안의 시간대별 수율 변화를 보여줍니다. 값이 낮아지는 구간은 검사 품질 저하 가능성이 있는 구간입니다.',
      legend: _legendRow([
        _legendItem('YIELD', const Color(0xFF42A5F5)),
      ]),
      summary: dropSummary,
      chart: SizedBox(
        height: 220,
        child: LineChart(
            LineChartData(
              minY: 0,
              maxY: 100,
              gridData: const FlGridData(show: true),
              borderData: FlBorderData(show: false),
              titlesData: FlTitlesData(
                topTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: false),
                ),
                rightTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: false),
                ),
                leftTitles: const AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 36,
                  ),
                ),
                bottomTitles: AxisTitles(
                  sideTitles: SideTitles(
                    showTitles: true,
                    reservedSize: 28,
                    interval: (data.length / 4).ceilToDouble(),
                    getTitlesWidget: (value, meta) {
                      final i = value.toInt();
                      return Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text(
                          timeLabel(i),
                          style: const TextStyle(
                            fontSize: 9,
                            color: Colors.white54,
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ),
              lineBarsData: [
                LineChartBarData(
                  spots: spots,
                  isCurved: true,
                  dotData: const FlDotData(show: true),
                  barWidth: 2,
                  color: const Color(0xFF42A5F5),
                  belowBarData: BarAreaData(
                    show: true,
                    color: const Color(0xFF42A5F5).withOpacity(0.12),
                  ),
                ),

                if (dropIndex != null && biggestDrop <= -p.dropThreshold)
                  LineChartBarData(
                    spots: [spots[dropIndex]],
                    isCurved: false,
                    barWidth: 0,
                    dotData: const FlDotData(show: true),
                    color: const Color(0xFFE53935),
                  ),
                if (abnormalSpots.isNotEmpty)
                  LineChartBarData(
                    spots: abnormalSpots,
                    isCurved: false,
                    barWidth: 0,
                    dotData: const FlDotData(show: true),
                    color: const Color(0xFFFF5252),
                  ),
              ],
            ),
          ),
      ),
    );
  }


  DateTime? _readShotTime(dynamic shot) {
    final raw = shot['StartTIme']?.toString() ??
        shot['StartTime']?.toString() ??
        shot['startTime']?.toString() ??
        shot['EndTime']?.toString() ??
        shot['CreatedAt']?.toString() ??
        shot['createdAt']?.toString() ??
        shot['created_at']?.toString() ??
        shot['Time']?.toString() ??
        shot['time']?.toString();

    if (raw == null || raw.isEmpty) return null;

    // "2026-05-01 00:26:50.5181782" 대응
    final normalized = raw.replaceFirst(' ', 'T');

    return DateTime.tryParse(normalized);
  }

  bool _isPassItem(String item) => item == 'PASS';

  bool _isNoDeviceItem(String item) => item.contains('NoDevice');

  bool _isXOutItem(String item) => item.contains('XOut');

  bool _isRejectItem(String item) {
    return !_isPassItem(item) && !_isNoDeviceItem(item) && !_isXOutItem(item);
  }

  Widget _alertContextCard(InspectionProvider p) {
    final data = p.series;

    if (data.isEmpty) {
      return const Card(
        margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Text('분석할 검사 데이터 없음'),
        ),
      );
    }

    dynamic targetShot;

    for (final shot in data.reversed) {
      final item = shot['Item']?.toString() ?? '';
      if (_isRejectItem(item)) {
        targetShot = shot;
        break;
      }
    }

    targetShot ??= data.last;

    final targetTime = _readShotTime(targetShot);

    if (targetTime == null) {
      return const Card(
        margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Text('시간 정보가 없어 주변 컨텍스트를 계산할 수 없음'),
        ),
      );
    }

    final from = targetTime.subtract(const Duration(minutes: 5));
    final to = targetTime.add(const Duration(minutes: 5));

    final nearby = data.where((shot) {
      final t = _readShotTime(shot);
      if (t == null) return false;
      return !t.isBefore(from) && !t.isAfter(to);
    }).toList();

    int total = 0;
    int pass = 0;
    int reject = 0;
    int noDevice = 0;
    int xout = 0;

    final typeCount = <int, int>{};
    final typeNgCount = <int, int>{};

    for (final shot in nearby) {
      total++;

      final item = shot['Item']?.toString() ?? '';
      final type = shot['InspectionType'] as int? ?? 0;

      typeCount[type] = (typeCount[type] ?? 0) + 1;

      if (_isPassItem(item)) {
        pass++;
      } else if (_isNoDeviceItem(item)) {
        noDevice++;
      } else if (_isXOutItem(item)) {
        xout++;
      } else {
        reject++;
        typeNgCount[type] = (typeNgCount[type] ?? 0) + 1;
      }
    }

    final denom = total - noDevice - xout;
    final yield = denom > 0 ? pass / denom * 100 : 0.0;

    int? mainNgType;
    int mainNgCount = 0;

    for (final entry in typeNgCount.entries) {
      if (entry.value > mainNgCount) {
        mainNgType = entry.key;
        mainNgCount = entry.value;
      }
    }

    String typeLabel(int? type) {
      switch (type) {
        case 1:
          return 'MARK';
        case 3:
          return 'BGA';
        case 5:
          return '2D CODE';
        default:
          return type == null ? '-' : 'TYPE $type';
      }
    }

    String timeText(DateTime t) {
      final h = t.hour.toString().padLeft(2, '0');
      final m = t.minute.toString().padLeft(2, '0');
      final s = t.second.toString().padLeft(2, '0');
      return '$h:$m:$s';
    }

    final targetItem = targetShot['Item']?.toString() ?? '-';

    final statusColor = yield >= 99
        ? const Color(0xFF43A047)
        : yield >= 95
        ? Colors.orange
        : const Color(0xFFE53935);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  _isRejectItem(targetItem)
                      ? Icons.warning_amber_rounded
                      : Icons.info_outline,
                  size: 18,
                  color: _isRejectItem(targetItem)
                      ? const Color(0xFFE53935)
                      : Colors.white54,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    '기준 Shot: $targetItem / ${timeText(targetTime)}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '분석 범위: ${timeText(from)} ~ ${timeText(to)}',
              style: const TextStyle(fontSize: 11, color: Colors.white54),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(child: _miniMetric('TOTAL', '$total', Colors.white70)),
                Expanded(child: _miniMetric('PASS', '$pass', const Color(0xFF43A047))),
                Expanded(child: _miniMetric('REJECT', '$reject', const Color(0xFFE53935))),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Expanded(child: _miniMetric('NO DEVICE', '$noDevice', Colors.grey)),
                Expanded(child: _miniMetric('X-OUT', '$xout', Colors.orange)),
                Expanded(
                  child: _miniMetric(
                    'YIELD',
                    '${yield.toStringAsFixed(1)}%',
                    statusColor,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white10,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                mainNgType == null
                    ? '이 구간에서는 뚜렷한 NG 집중 타입이 없습니다.'
                    : '주요 NG 타입은 ${typeLabel(mainNgType)}이며, 주변 구간에서 $mainNgCount건 발생했습니다.',
                style: const TextStyle(fontSize: 12),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _miniMetric(String label, String value, Color color) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 10, color: Colors.white54),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
      ],
    );
  }

  //시간대별 검사 결과 분포 그래프
  // 시간대별 검사 결과 분포 그래프
  Widget _resultTrendChart(InspectionProvider p) {
    final data = p.series;

    final Map<String, Map<String, int>> bucket = {};

    for (final shot in data) {
      final t = _readShotTime(shot);
      if (t == null) continue;

      final key =
          '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

      bucket.putIfAbsent(key, () => {
        'PASS': 0,
        'REJECT': 0,
        'NODEVICE': 0,
        'XOUT': 0,
      });

      final item = shot['Item']?.toString() ?? '';

      if (item == 'PASS') {
        bucket[key]!['PASS'] = bucket[key]!['PASS']! + 1;
      } else if (item.contains('NoDevice')) {
        bucket[key]!['NODEVICE'] = bucket[key]!['NODEVICE']! + 1;
      } else if (item.contains('XOut')) {
        bucket[key]!['XOUT'] = bucket[key]!['XOUT']! + 1;
      } else {
        bucket[key]!['REJECT'] = bucket[key]!['REJECT']! + 1;
      }
    }

    final keys = bucket.keys.toList()..sort();

    if (keys.isEmpty) {
      return _chartCard(
        title: '시간대별 검사 결과 분포',
        description: '선택한 기간에 검사 결과 데이터가 없습니다.',
        chart: const SizedBox(
          height: 80,
          child: Center(child: Text('데이터 없음')),
        ),
      );
    }

    final passSpots = <FlSpot>[];
    final rejectSpots = <FlSpot>[];
    final noDeviceSpots = <FlSpot>[];
    final xoutSpots = <FlSpot>[];

    for (int i = 0; i < keys.length; i++) {
      final k = keys[i];
      passSpots.add(FlSpot(i.toDouble(), bucket[k]!['PASS']!.toDouble()));
      rejectSpots.add(FlSpot(i.toDouble(), bucket[k]!['REJECT']!.toDouble()));
      noDeviceSpots.add(FlSpot(i.toDouble(), bucket[k]!['NODEVICE']!.toDouble()));
      xoutSpots.add(FlSpot(i.toDouble(), bucket[k]!['XOUT']!.toDouble()));
    }

    final interval = keys.length <= 4 ? 1.0 : (keys.length / 4).ceilToDouble();

    return _chartCard(
      title: '시간대별 검사 결과 분포',
      description:
      '같은 시간대에 발생한 검사 결과를 PASS, REJECT, NO DEVICE, X-OUT으로 나누어 보여줍니다. 특정 시간에 REJECT가 몰리면 장비나 공정 상태를 확인해야 합니다.',
      legend: _legendRow([
        _legendItem('PASS', const Color(0xFF43A047)),
        _legendItem('REJECT', const Color(0xFFE53935)),
        _legendItem('NO DEVICE', Colors.grey),
        _legendItem('X-OUT', Colors.orange),
      ]),
      summary: 'REJECT가 많이 튄 시간대가 있으면 해당 구간의 Shot 상세와 이벤트 로그를 함께 확인하세요.',
      chart: SizedBox(
        height: 220,
        child: LineChart(
          LineChartData(
            minY: 0,
            gridData: const FlGridData(show: true),
            borderData: FlBorderData(show: false),
            titlesData: FlTitlesData(
              topTitles: const AxisTitles(
                sideTitles: SideTitles(showTitles: false),
              ),
              rightTitles: const AxisTitles(
                sideTitles: SideTitles(showTitles: false),
              ),
              leftTitles: const AxisTitles(
                sideTitles: SideTitles(showTitles: true, reservedSize: 36),
              ),
              bottomTitles: AxisTitles(
                sideTitles: SideTitles(
                  showTitles: true,
                  reservedSize: 28,
                  interval: interval,
                  getTitlesWidget: (v, _) {
                    final i = v.toInt();
                    if (i < 0 || i >= keys.length) return const SizedBox();
                    return Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Text(
                        keys[i],
                        style: const TextStyle(
                          fontSize: 9,
                          color: Colors.white54,
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
            lineBarsData: [
              LineChartBarData(
                spots: passSpots,
                color: const Color(0xFF43A047),
                isCurved: true,
                barWidth: 2,
                dotData: const FlDotData(show: false),
              ),
              LineChartBarData(
                spots: rejectSpots,
                color: const Color(0xFFE53935),
                isCurved: true,
                barWidth: 2,
                dotData: const FlDotData(show: true),
              ),
              LineChartBarData(
                spots: noDeviceSpots,
                color: Colors.grey,
                isCurved: true,
                barWidth: 2,
                dotData: const FlDotData(show: false),
              ),
              LineChartBarData(
                spots: xoutSpots,
                color: Colors.orange,
                isCurved: true,
                barWidth: 2,
                dotData: const FlDotData(show: false),
              ),
            ],
          ),
        ),
      ),
    );
  }

  //
  Widget _chartCard({
    required String title,
    required String description,
    required Widget chart,
    Widget? legend,
    String? summary,
  }) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 10),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              description,
              style: const TextStyle(
                fontSize: 11,
                color: Colors.white54,
              ),
            ),
            if (legend != null) ...[
              const SizedBox(height: 8),
              legend,
            ],
            const SizedBox(height: 12),
            chart,
            if (summary != null) ...[
              const SizedBox(height: 10),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.white10,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  summary,
                  style: const TextStyle(fontSize: 11, color: Colors.white70),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _legendItem(String label, Color color) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(Icons.circle, size: 8, color: color),
        const SizedBox(width: 4),
        Text(
          label,
          style: const TextStyle(fontSize: 10, color: Colors.white70),
        ),
      ],
    );
  }

  Widget _legendRow(List<Widget> items) {
    return Wrap(
      spacing: 12,
      runSpacing: 6,
      children: items,
    );
  }

  //검사 타입별 NG 변화
  Widget _typeNgTrendChart(InspectionProvider p) {
    final data = p.series;

    final Map<String, Map<int, List<String>>> bucket = {};

    for (final shot in data) {
      final t = _readShotTime(shot);
      if (t == null) continue;

      final key =
          '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}';

      bucket.putIfAbsent(key, () => {});

      final type = shot['InspectionType'] as int? ?? 0;
      final item = shot['Item']?.toString() ?? '';

      bucket[key]!.putIfAbsent(type, () => []).add(item);
    }

    final keys = bucket.keys.toList()..sort();

    if (keys.isEmpty) {
      return _chartCard(
        title: '검사 타입별 NG율 변화',
        description: '선택한 기간에 검사 타입별 데이터가 없습니다.',
        chart: const SizedBox(
          height: 80,
          child: Center(child: Text('데이터 없음')),
        ),
      );
    }

    List<FlSpot> makeSpots(int type) {
      final spots = <FlSpot>[];

      for (int i = 0; i < keys.length; i++) {
        final k = keys[i];
        final items = bucket[k]![type] ?? [];

        if (items.isEmpty) continue;

        final ng = items.where((e) => e != 'PASS').length;
        final rate = ng / items.length * 100;

        spots.add(FlSpot(i.toDouble(), rate));
      }

      return spots;
    }

    final markSpots = makeSpots(1);
    final bgaSpots = makeSpots(3);
    final codeSpots = makeSpots(5);

    final interval = keys.length <= 4 ? 1.0 : (keys.length / 4).ceilToDouble();

    return _chartCard(
      title: '검사 타입별 NG율 변화',
      description:
      'MARK, BGA, 2D CODE 검사별로 시간대별 NG 비율을 비교합니다. 특정 타입만 높아지면 해당 검사 모듈 또는 조건 문제일 가능성이 큽니다.',
      legend: _legendRow([
        _legendItem('MARK', const Color(0xFFAB47BC)),
        _legendItem('BGA', Colors.orange),
        _legendItem('2D CODE', const Color(0xFF42A5F5)),
      ]),
      summary: '한 검사 타입의 NG율만 반복적으로 높다면 해당 검사 항목을 우선 점검하세요.',
      chart: SizedBox(
        height: 220,
        child: LineChart(
          LineChartData(
            minY: 0,
            maxY: 100,
            gridData: const FlGridData(show: true),
            borderData: FlBorderData(show: false),
            titlesData: FlTitlesData(
              topTitles: const AxisTitles(
                sideTitles: SideTitles(showTitles: false),
              ),
              rightTitles: const AxisTitles(
                sideTitles: SideTitles(showTitles: false),
              ),
              leftTitles: const AxisTitles(
                sideTitles: SideTitles(
                  showTitles: true,
                  reservedSize: 36,
                ),
              ),
              bottomTitles: AxisTitles(
                sideTitles: SideTitles(
                  showTitles: true,
                  reservedSize: 28,
                  interval: interval,
                  getTitlesWidget: (v, _) {
                    final i = v.toInt();
                    if (i < 0 || i >= keys.length) return const SizedBox();

                    return Padding(
                      padding: const EdgeInsets.only(top: 6),
                      child: Text(
                        keys[i],
                        style: const TextStyle(
                          fontSize: 9,
                          color: Colors.white54,
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
            lineBarsData: [
              if (markSpots.isNotEmpty)
                LineChartBarData(
                  spots: markSpots,
                  color: const Color(0xFFAB47BC),
                  isCurved: true,
                  barWidth: 2,
                  dotData: const FlDotData(show: true),
                ),
              if (bgaSpots.isNotEmpty)
                LineChartBarData(
                  spots: bgaSpots,
                  color: Colors.orange,
                  isCurved: true,
                  barWidth: 2,
                  dotData: const FlDotData(show: true),
                ),
              if (codeSpots.isNotEmpty)
                LineChartBarData(
                  spots: codeSpots,
                  color: const Color(0xFF42A5F5),
                  isCurved: true,
                  barWidth: 2,
                  dotData: const FlDotData(show: true),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _rangeSelector(InspectionProvider p) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          _rangeBtn('1H', RangeType.hour1, p),
          _rangeBtn('6H', RangeType.hour6, p),
          _rangeBtn('TODAY', RangeType.today, p),
        ],
      ),
    );
  }

  Widget _rangeBtn(String label, RangeType type, InspectionProvider p) {
    final selected = p.rangeType == type;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: TextButton(
        onPressed: () => p.setRange(type),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: selected ? FontWeight.bold : FontWeight.normal,
            color: selected ? Colors.white : Colors.white38,
          ),
        ),
      ),
    );
  }
  Widget _dropThresholdSelector(InspectionProvider p) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 4),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          child: Row(
            children: [
              const Icon(
                Icons.trending_down,
                size: 16,
                color: Colors.white54,
              ),
              const SizedBox(width: 8),
              const Text(
                '급락 기준',
                style: TextStyle(fontSize: 12, color: Colors.white70),
              ),
              const Spacer(),
              _dropBtn('3%p', 3.0, p),
              _dropBtn('5%p', 5.0, p),
              _dropBtn('10%p', 10.0, p),
            ],
          ),
        ),
      ),
    );
  }
  Widget _dropBtn(String label, double value, InspectionProvider p) {
    final selected = p.dropThreshold == value;

    return Padding(
      padding: const EdgeInsets.only(left: 6),
      child: TextButton(
        onPressed: () => p.setDropThreshold(value),
        style: TextButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          minimumSize: Size.zero,
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
          backgroundColor: selected ? const Color(0xFFE53935) : Colors.white10,
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 11,
            fontWeight: selected ? FontWeight.bold : FontWeight.normal,
            color: selected ? Colors.white : Colors.white60,
          ),
        ),
      ),
    );
  }
  Widget _abnormalThresholdSelector(InspectionProvider p) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 0, 12, 4),
      child: Card(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          child: Row(
            children: [
              const Icon(
                Icons.warning_amber_rounded,
                size: 16,
                color: Colors.white54,
              ),
              const SizedBox(width: 8),
              const Text(
                '이상 기준',
                style: TextStyle(fontSize: 12, color: Colors.white70),
              ),
              const Spacer(),
              _abnormalBtn('85%', 85.0, p),
              _abnormalBtn('90%', 90.0, p),
              _abnormalBtn('95%', 95.0, p),
            ],
          ),
        ),
      ),
    );
  }
  Widget _abnormalBtn(String label, double value, InspectionProvider p) {
    final selected = p.abnormalYieldThreshold == value;

    return Padding(
      padding: const EdgeInsets.only(left: 6),
      child: TextButton(
        onPressed: () => p.setAbnormalYieldThreshold(value),
        style: TextButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          minimumSize: Size.zero,
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
          backgroundColor:
          selected ? const Color(0xFFFF9800) : Colors.white10,
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 11,
            fontWeight: selected ? FontWeight.bold : FontWeight.normal,
            color: selected ? Colors.white : Colors.white60,
          ),
        ),
      ),
    );
  }
}