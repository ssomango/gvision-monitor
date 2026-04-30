import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/ws/ws_client.dart';
import 'home_provider.dart';
import 'widgets/status_banner.dart';
import 'widgets/event_log_tile.dart';
import '../events/event_context_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<HomeProvider>();
    final ws = context.watch<WsClient>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('GVision Monitor'),
        actions: [
          // 연결 상태 표시
          _WsIndicator(state: ws.state),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: provider.refresh,
          ),
        ],
      ),
      body: provider.loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                // 서버 연결 안 됐을 때 경고 배너
                if (ws.state == WsState.disconnected)
                  _connectionWarning(context),

                // 장비 상태 배너
                Padding(
                  padding: const EdgeInsets.all(12),
                  child: StatusBanner(status: provider.status),
                ),

                // 이벤트 로그 헤더
                Padding(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 16, vertical: 4),
                  child: Row(
                    children: [
                      const Icon(Icons.list_alt, size: 16),
                      const SizedBox(width: 6),
                      const Text('최근 이벤트 로그',
                          style: TextStyle(fontWeight: FontWeight.bold)),
                      const Spacer(),
                      Text(
                        '${provider.recentEvents.length}건',
                        style: const TextStyle(
                            fontSize: 12, color: Colors.white54),
                      ),
                    ],
                  ),
                ),
                const Divider(height: 1),

                // 이벤트 목록
                Expanded(
                  child: provider.recentEvents.isEmpty
                      ? const Center(
                          child: Text('이벤트 없음',
                              style: TextStyle(color: Colors.white38)))
                      : ListView.separated(
                          itemCount: provider.recentEvents.length,
                          separatorBuilder: (_, __) =>
                              const Divider(height: 1, indent: 16),
                          itemBuilder: (ctx, i) {
                            final e = provider.recentEvents[i];
                            return EventLogTile(
                              event: e,
                              onTap: e.isAlert
                                  ? () => Navigator.push(
                                        ctx,
                                        MaterialPageRoute(
                                          builder: (_) => EventContextScreen(
                                              eventId: e.id),
                                        ),
                                      )
                                  : null,
                            );
                          },
                        ),
                ),
              ],
            ),
    );
  }

  Widget _connectionWarning(BuildContext context) => Container(
        width: double.infinity,
        color: Colors.red.shade900,
        padding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Row(
          children: [
            const Icon(Icons.wifi_off, size: 16, color: Colors.white70),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                '서버 연결 안 됨 — 설정에서 IP를 확인하세요',
                style: const TextStyle(fontSize: 12, color: Colors.white70),
              ),
            ),
          ],
        ),
      );
}

class _WsIndicator extends StatelessWidget {
  final WsState state;
  const _WsIndicator({required this.state});

  @override
  Widget build(BuildContext context) {
    final (icon, color, tooltip) = switch (state) {
      WsState.connected => (Icons.wifi, Colors.greenAccent, '실시간 연결됨'),
      WsState.connecting => (Icons.wifi_find, Colors.orange, '연결 중...'),
      WsState.disconnected => (Icons.wifi_off, Colors.red, '연결 끊김'),
    };
    return Tooltip(
      message: tooltip,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8),
        child: Icon(icon, color: color, size: 20),
      ),
    );
  }
}
