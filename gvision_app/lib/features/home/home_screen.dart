import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../inspection/inspection_provider.dart';
import '../../core/ws/ws_client.dart';
import 'home_provider.dart';
import 'widgets/status_banner.dart';
import 'widgets/event_log_tile.dart';
import 'widgets/yield_gauge_card.dart';
import '../events/event_context_screen.dart';
import '../../shared/widgets/dashboard_card.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<HomeProvider>();
    final ws = context.watch<WsClient>();
    final p = context.watch<InspectionProvider>();

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
          : Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            if (ws.state == WsState.disconnected)
              _connectionWarning(context),

            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  flex: 5,
                  child: StatusBanner(status: provider.status),
                ),
                const SizedBox(width: 12),
                Expanded(
                  flex: 4,
                  child: YieldGaugeCard(
                    yieldRate: p.yieldRate,
                    total: p.total,
                    good: p.good,
                    reject: p.reject,
                    noDevice: p.noDevice,
                  ),
                ),
              ],
            ),

            const SizedBox(height: 12),

            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 5,
                    child: _homeContextPanel(p, ws),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 6,
                    child: _eventPanel(context, provider),
                  ),
                ],
              ),
            ),
          ],
        ),
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

  Widget _homeContextPanel(InspectionProvider p, WsClient ws) {
    final yieldColor = p.yieldRate >= 99
        ? const Color(0xFF43A047)
        : p.yieldRate >= 95
        ? Colors.orange
        : const Color(0xFFE53935);

    final statusText = ws.state == WsState.connected
        ? '실시간 연결 정상'
        : ws.state == WsState.connecting
        ? '서버 연결 중'
        : '서버 연결 끊김';

    final message = p.yieldRate >= 99
        ? '현재 검사 수율이 안정적입니다.'
        : p.yieldRate >= 95
        ? '수율이 주의 구간입니다. 최근 이벤트와 검사 타입별 NG율을 확인하세요.'
        : '수율이 위험 구간입니다. Inspection 분석에서 이상 Shot 주변 컨텍스트를 확인하세요.';

    return DashboardCard(
      title: '현장 요약',
      description: '현재 장비 연결 상태와 검사 품질 상태를 요약합니다.',
      icon: Icons.dashboard_customize,
      margin: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _contextRow('서버 상태', statusText, Colors.white70),
          _contextRow('현재 수율', '${p.yieldRate.toStringAsFixed(1)}%', yieldColor),
          _contextRow('총 검사', '${p.total}', Colors.white70),
          _contextRow('REJECT', '${p.reject}', const Color(0xFFE53935)),
          _contextRow('NO DEVICE', '${p.noDevice}', Colors.grey),
          const SizedBox(height: 16),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: yieldColor.withOpacity(0.12),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: yieldColor.withOpacity(0.35)),
            ),
            child: Text(
              message,
              style: TextStyle(fontSize: 12, color: yieldColor, height: 1.35),
            ),
          ),
        ],
      ),
    );
  }

  Widget _contextRow(String label, String value, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          SizedBox(
            width: 90,
            child: Text(
              label,
              style: const TextStyle(fontSize: 12, color: Colors.white54),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: color,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _eventPanel(BuildContext context, HomeProvider provider) {
    return DashboardCard(
      title: '최근 이벤트 로그',
      description: '최근 발생한 장비/검사/LOT/Recipe 이벤트를 확인합니다.',
      icon: Icons.list_alt,
      margin: EdgeInsets.zero,
      child: Expanded(
        child: provider.recentEvents.isEmpty
            ? const Center(
          child: Text(
            '이벤트 없음',
            style: TextStyle(color: Colors.white38),
          ),
        )
            : ListView.separated(
          itemCount: provider.recentEvents.length,
          separatorBuilder: (_, __) => const Divider(height: 1),
          itemBuilder: (ctx, i) {
            final e = provider.recentEvents[i];
            return EventLogTile(
              event: e,
              onTap: e.isAlert
                  ? () => Navigator.push(
                ctx,
                MaterialPageRoute(
                  builder: (_) => EventContextScreen(eventId: e.id),
                ),
              )
                  : null,
            );
          },
        ),
      ),
    );
  }
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
