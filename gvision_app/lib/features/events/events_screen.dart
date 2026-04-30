import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/models/event.dart';
import '../../shared/theme.dart';
import 'events_provider.dart';
import 'event_context_screen.dart';

class EventsScreen extends StatefulWidget {
  const EventsScreen({super.key});

  @override
  State<EventsScreen> createState() => _EventsScreenState();
}

class _EventsScreenState extends State<EventsScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;
  final _filters = [null, 1, 2, 4, 5];
  final _labels = ['전체', '시스템', '검사', 'LOT', 'Recipe'];

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: _filters.length, vsync: this);
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<EventsProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('이벤트 분석'),
        actions: [
          IconButton(
              icon: const Icon(Icons.refresh), onPressed: provider.refresh),
        ],
        bottom: TabBar(
          controller: _tabs,
          isScrollable: true,
          tabAlignment: TabAlignment.start,
          tabs: _labels.map((l) => Tab(text: l)).toList(),
          onTap: (i) => provider.filterBy(_filters[i]),
        ),
      ),
      body: provider.loading
          ? const Center(child: CircularProgressIndicator())
          : provider.events.isEmpty
              ? const Center(
                  child: Text('이벤트 없음',
                      style: TextStyle(color: Colors.white38)))
              : ListView.separated(
                  itemCount: provider.events.length,
                  separatorBuilder: (_, __) =>
                      const Divider(height: 1, indent: 16),
                  itemBuilder: (ctx, i) =>
                      _buildTile(ctx, provider.events[i]),
                ),
    );
  }

  Widget _buildTile(BuildContext ctx, GvisionEvent event) {
    final color = AppTheme.logTypeColor(event.logType);
    return InkWell(
      onTap: event.isAlert
          ? () => Navigator.push(
                ctx,
                MaterialPageRoute(
                  builder: (_) => EventContextScreen(eventId: event.id),
                ),
              )
          : null,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
                width: 3,
                height: 40,
                color: color,
                margin: const EdgeInsets.only(right: 10)),
            Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(3),
              ),
              child: Text(event.logTypeLabel,
                  style: TextStyle(
                      fontSize: 10,
                      color: color,
                      fontWeight: FontWeight.bold)),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(event.description,
                      style: const TextStyle(fontSize: 13),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 2),
                  Text(_formatTime(event.time),
                      style: const TextStyle(
                          fontSize: 11, color: Colors.white38)),
                ],
              ),
            ),
            if (event.isAlert)
              const Padding(
                padding: EdgeInsets.only(left: 4),
                child: Icon(Icons.chevron_right,
                    size: 18, color: Colors.white38),
              ),
          ],
        ),
      ),
    );
  }

  String _formatTime(String raw) {
    if (raw.isEmpty) return '';
    try {
      final dt = DateTime.parse(raw).toLocal();
      return '${dt.month.toString().padLeft(2, '0')}-'
          '${dt.day.toString().padLeft(2, '0')} '
          '${dt.hour.toString().padLeft(2, '0')}:'
          '${dt.minute.toString().padLeft(2, '0')}';
    } catch (_) {
      return raw.length > 13 ? raw.substring(0, 13) : raw;
    }
  }
}
