import 'package:flutter/material.dart';
import '../../core/api/lots_api.dart';
import '../../core/models/lot.dart';
import '../../shared/widgets/dashboard_card.dart';

class LotsScreen extends StatefulWidget {
  const LotsScreen({super.key});


  @override
  State<LotsScreen> createState() => _LotsScreenState();
}

class _LotsScreenState extends State<LotsScreen> {
  List<Lot> _lots = [];
  bool _loading = true;

  Lot? _selectedLot;
  LotStats? _selectedStats;
  bool _statsLoading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final lots = await LotsApi.fetchLots(limit: 30);
      setState(() {
        _lots = lots;
        _loading = false;
      });
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  Future<void> _selectLot(Lot lot) async {
    setState(() {
      _selectedLot = lot;
      _selectedStats = null;
      _statsLoading = true;
    });

    try {
      final stats = await LotsApi.fetchStats(lot.id);
      if (!mounted) return;
      setState(() {
        _selectedStats = stats;
        _statsLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _statsLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Lot 이력'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _lots.isEmpty
          ? const Center(
        child: Text(
          'Lot 없음',
          style: TextStyle(color: Colors.white38),
        ),
      )
          : Row(
        children: [
          Expanded(
            flex: 5,
            child: _lotListPanel(),
          ),
          Container(width: 1, color: Colors.white12),
          Expanded(
            flex: 5,
            child: _lotDetailPanel(),
          ),
        ],
      ),
    );
  }


  Widget _lotListPanel() {
    return Card(
      margin: const EdgeInsets.all(12),
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 14, 16, 4),
            child: Row(
              children: [
                Icon(Icons.history, size: 18, color: Color(0xFF90CAF9)),
                SizedBox(width: 8),
                Text(
                  'Lot 목록',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                ),
              ],
            ),
          ),
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 16),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Text(
                '최근 생산 Lot을 선택하면 우측에서 품질 요약을 확인할 수 있습니다.',
                style: TextStyle(fontSize: 11, color: Colors.white54),
              ),
            ),
          ),
          const SizedBox(height: 10),
          const Divider(height: 1),
          Expanded(
            child: ListView.separated(
              itemCount: _lots.length,
              separatorBuilder: (_, __) => const Divider(height: 1),
              itemBuilder: (ctx, i) {
                final lot = _lots[i];
                final selected = _selectedLot?.id == lot.id;

                return InkWell(
                  onTap: () => _selectLot(lot),
                  child: Container(
                    color: selected
                        ? const Color(0xFF1976D2).withOpacity(0.14)
                        : Colors.transparent,
                    padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                    child: Row(
                      children: [
                        Icon(
                          selected
                              ? Icons.radio_button_checked
                              : Icons.radio_button_unchecked,
                          size: 18,
                          color: selected
                              ? const Color(0xFF90CAF9)
                              : Colors.white38,
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                lot.lotNo,
                                style: TextStyle(
                                  fontWeight:
                                  selected ? FontWeight.bold : FontWeight.w500,
                                  fontSize: 13,
                                ),
                              ),
                              const SizedBox(height: 3),
                              Text(
                                lot.recipeName ?? '-',
                                style: const TextStyle(
                                  fontSize: 11,
                                  color: Colors.white54,
                                ),
                              ),
                            ],
                          ),
                        ),
                        Text(
                          _formatDate(lot.startTime),
                          style: const TextStyle(
                            fontSize: 11,
                            color: Colors.white38,
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _lotDetailPanel() {
    final lot = _selectedLot;

    if (lot == null) {
      return const Card(
        margin: EdgeInsets.all(12),
        child: Center(
          child: Text(
            'Lot을 선택하면 상세 품질 정보가 표시됩니다.',
            style: TextStyle(color: Colors.white54),
          ),
        ),
      );
    }

    if (_statsLoading) {
      return const Card(
        margin: EdgeInsets.all(12),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    final stats = _selectedStats;

    if (stats == null) {
      return const Card(
        margin: EdgeInsets.all(12),
        child: Center(
          child: Text(
            'Lot 통계를 불러오지 못했습니다.',
            style: TextStyle(color: Colors.white54),
          ),
        ),
      );
    }

    final yieldColor = stats.yield_ >= 99
        ? const Color(0xFF43A047)
        : stats.yield_ >= 95
        ? Colors.orange
        : const Color(0xFFE53935);

    final status = stats.yield_ >= 99
        ? '정상'
        : stats.yield_ >= 95
        ? '주의'
        : '위험';

    return DashboardCard(
      title: 'Lot 품질 요약',
      description: '${lot.lotNo} / ${lot.recipeName ?? '-'}',
      icon: Icons.fact_check,
      margin: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: _bigMetric(
                  '상태',
                  status,
                  yieldColor,
                  Icons.fact_check,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _bigMetric(
                  'YIELD',
                  '${stats.yield_.toStringAsFixed(1)}%',
                  yieldColor,
                  Icons.percent,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _bigMetric(
                  'TOTAL',
                  '${stats.total}',
                  Colors.white70,
                  Icons.inventory_2,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _bigMetric(
                  'GOOD',
                  '${stats.good}',
                  const Color(0xFF43A047),
                  Icons.check_circle,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: _bigMetric(
                  'REJECT',
                  '${stats.reject}',
                  const Color(0xFFE53935),
                  Icons.cancel,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _bigMetric(
                  'NO DEVICE',
                  '${stats.noDevice}',
                  Colors.grey,
                  Icons.remove_circle,
                ),
              ),
            ],
          ),
          const SizedBox(height: 18),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: yieldColor.withOpacity(0.10),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: yieldColor.withOpacity(0.3)),
            ),
            child: Text(
              stats.yield_ >= 99
                  ? '이 Lot은 안정적인 검사 품질을 보입니다.'
                  : stats.yield_ >= 95
                  ? '이 Lot은 수율 주의 구간입니다. REJECT와 NO DEVICE 비중을 확인하세요.'
                  : '이 Lot은 수율 위험 구간입니다. 해당 Lot의 검사 조건과 Recipe 변경 이력을 함께 확인하세요.',
              style: TextStyle(
                fontSize: 12,
                height: 1.4,
                color: yieldColor,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _bigMetric(
      String label,
      String value,
      Color color,
      IconData icon,
      ) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withOpacity(0.10),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: color.withOpacity(0.25)),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(fontSize: 10, color: Colors.white54),
                ),
                const SizedBox(height: 3),
                Text(
                  value,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: color,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _LotTile extends StatefulWidget {
  final Lot lot;
  const _LotTile({required this.lot});

  @override
  State<_LotTile> createState() => _LotTileState();
}

class _LotTileState extends State<_LotTile> {
  LotStats? _stats;
  bool _loading = false;

  Future<void> _loadStats() async {
    if (_stats != null) return;
    setState(() => _loading = true);
    try {
      final stats = await LotsApi.fetchStats(widget.lot.id);
      setState(() {
        _stats = stats;
        _loading = false;
      });
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ExpansionTile(
      title: Text(widget.lot.lotNo,
          style: const TextStyle(fontWeight: FontWeight.w500)),
      subtitle: Text(
        widget.lot.recipeName ?? '',
        style: const TextStyle(fontSize: 11, color: Colors.white54),
      ),
      trailing: Text(
        _formatDate(widget.lot.startTime),
        style: const TextStyle(fontSize: 11, color: Colors.white38),
      ),
      onExpansionChanged: (v) {
        if (v) _loadStats();
      },
      children: [
        if (_loading)
          const Padding(
            padding: EdgeInsets.all(16),
            child: CircularProgressIndicator(),
          )
        else if (_stats != null)
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: _buildStats(_stats!),
          ),
      ],
    );
  }

  Widget _buildStats(LotStats s) {
    final yieldColor = s.yield_ >= 99
        ? const Color(0xFF43A047)
        : s.yield_ >= 95
            ? Colors.orange
            : const Color(0xFFE53935);

    return Column(
      children: [
        Row(
          children: [
            _statChip('TOTAL', '${s.total}', Colors.white70),
            const SizedBox(width: 8),
            _statChip('GOOD', '${s.good}', const Color(0xFF43A047)),
            const SizedBox(width: 8),
            _statChip('REJECT', '${s.reject}', const Color(0xFFE53935)),
          ],
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            _statChip('NO DEV', '${s.noDevice}', Colors.grey),
            const SizedBox(width: 8),
            _statChip('X-OUT', '${s.xout}', Colors.orange),
            const SizedBox(width: 8),
            _statChip('YIELD', '${s.yield_.toStringAsFixed(1)}%', yieldColor),
          ],
        ),
      ],
    );
  }

  Widget _statChip(String label, String value, Color color) => Expanded(
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 8),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(6),
            border: Border.all(color: color.withValues(alpha: 0.3)),
          ),
          child: Column(
            children: [
              Text(label,
                  style: const TextStyle(fontSize: 9, color: Colors.white54)),
              const SizedBox(height: 2),
              Text(value,
                  style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: color)),
            ],
          ),
        ),
      );
}

String _formatDate(String raw) {
  if (raw.isEmpty) return '';
  try {
    final dt = DateTime.parse(raw).toLocal();
    return '${dt.month.toString().padLeft(2, '0')}.'
        '${dt.day.toString().padLeft(2, '0')}';
  } catch (_) {
    return raw.length > 10 ? raw.substring(0, 10) : raw;
  }
}
