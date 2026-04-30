import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:provider/provider.dart';
import 'inspection_provider.dart';
import '../../shared/widgets/stat_card.dart';

class InspectionScreen extends StatelessWidget {
  const InspectionScreen({super.key});

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
          : RefreshIndicator(
              onRefresh: p.refresh,
              child: ListView(
                children: [
                  _sectionHeader(context, '오늘 검사 결과'),
                  _summaryGrid(p),
                  _sectionHeader(context, '검사 타입별 현황'),
                  _typeTable(p),
                  if (p.errors.isNotEmpty) ...[
                    _sectionHeader(context, '불량 유형 Top10'),
                    _paretoChart(p.errors),
                  ],
                  const SizedBox(height: 24),
                ],
              ),
            ),
    );
  }

  Widget _sectionHeader(BuildContext context, String title) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
        child: Text(title,
            style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.bold,
                color: Colors.white70)),
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
            valueColor: const Color(0xFF43A047)),
        StatCard(
            label: 'NO DEVICE',
            value: '${p.noDevice}',
            icon: Icons.remove_circle,
            valueColor: Colors.grey),
        StatCard(
            label: 'REJECT',
            value: '${p.reject}',
            icon: Icons.cancel,
            valueColor: const Color(0xFFE53935)),
        StatCard(
            label: 'X-OUT',
            value: '${p.xout}',
            icon: Icons.block,
            valueColor: Colors.orange),
        StatCard(
            label: 'YIELD',
            value: '${p.yieldRate.toStringAsFixed(1)}%',
            icon: Icons.percent,
            valueColor: p.yieldRate >= 99
                ? const Color(0xFF43A047)
                : p.yieldRate >= 95
                    ? Colors.orange
                    : const Color(0xFFE53935)),
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
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: const [
                Expanded(
                    child: Text('타입',
                        style: TextStyle(fontSize: 11, color: Colors.white54))),
                SizedBox(
                    width: 60,
                    child: Text('검사',
                        textAlign: TextAlign.right,
                        style: TextStyle(fontSize: 11, color: Colors.white54))),
                SizedBox(
                    width: 60,
                    child: Text('NG',
                        textAlign: TextAlign.right,
                        style: TextStyle(fontSize: 11, color: Colors.white54))),
                SizedBox(
                    width: 70,
                    child: Text('NG율',
                        textAlign: TextAlign.right,
                        style: TextStyle(fontSize: 11, color: Colors.white54))),
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
                  style: const TextStyle(fontWeight: FontWeight.w500))),
          SizedBox(
              width: 60,
              child: Text('$total',
                  textAlign: TextAlign.right,
                  style: const TextStyle(fontSize: 13))),
          SizedBox(
              width: 60,
              child: Text('$ng',
                  textAlign: TextAlign.right,
                  style: TextStyle(
                      fontSize: 13,
                      color: ng > 0 ? const Color(0xFFE53935) : null))),
          SizedBox(
              width: 70,
              child: Text('${rate.toStringAsFixed(2)}%',
                  textAlign: TextAlign.right,
                  style: TextStyle(fontSize: 13, color: color))),
        ],
      ),
    );
  }

  Widget _paretoChart(List<dynamic> errors) {
    final sorted = [...errors]
      ..sort((a, b) => (b['count'] as int).compareTo(a['count'] as int));
    final top = sorted.take(10).toList();

    return SizedBox(
      height: 220,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 8, 24, 8),
        child: BarChart(
          BarChartData(
            alignment: BarChartAlignment.spaceAround,
            barGroups: top.asMap().entries.map((e) {
              final count = (e.value['count'] as int).toDouble();
              return BarChartGroupData(
                x: e.key,
                barRods: [
                  BarChartRodData(
                      toY: count,
                      color: const Color(0xFF42A5F5),
                      width: 18,
                      borderRadius: const BorderRadius.vertical(
                          top: Radius.circular(3))),
                ],
              );
            }).toList(),
            titlesData: FlTitlesData(
              bottomTitles: AxisTitles(
                sideTitles: SideTitles(
                  showTitles: true,
                  getTitlesWidget: (v, _) {
                    final i = v.toInt();
                    if (i >= top.length) return const SizedBox();
                    final label = (top[i]['resultType'] as String? ?? '')
                        .replaceAll(RegExp(r'(?<=[a-z])(?=[A-Z])'), ' ');
                    return Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(
                        label.length > 8 ? '${label.substring(0, 8)}..' : label,
                        style: const TextStyle(fontSize: 8),
                        textAlign: TextAlign.center,
                      ),
                    );
                  },
                  reservedSize: 32,
                ),
              ),
              leftTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: true, reservedSize: 36)),
              topTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: false)),
              rightTitles: const AxisTitles(
                  sideTitles: SideTitles(showTitles: false)),
            ),
            gridData: const FlGridData(show: true),
            borderData: FlBorderData(show: false),
          ),
        ),
      ),
    );
  }
}
