import 'package:flutter/material.dart';
import '../../core/api/lots_api.dart';
import '../../core/models/lot.dart';

class LotsScreen extends StatefulWidget {
  const LotsScreen({super.key});

  @override
  State<LotsScreen> createState() => _LotsScreenState();
}

class _LotsScreenState extends State<LotsScreen> {
  List<Lot> _lots = [];
  bool _loading = true;

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
                  child: Text('Lot 없음',
                      style: TextStyle(color: Colors.white38)))
              : ListView.separated(
                  itemCount: _lots.length,
                  separatorBuilder: (_, __) =>
                      const Divider(height: 1, indent: 16),
                  itemBuilder: (ctx, i) => _LotTile(lot: _lots[i]),
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
}
