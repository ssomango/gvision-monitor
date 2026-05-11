import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _hostCtrl;
  late final TextEditingController _portCtrl;

  @override
  void initState() {
    super.initState();
    _hostCtrl = TextEditingController(text: ApiClient.host);
    _portCtrl = TextEditingController(text: '${ApiClient.port}');
  }

  @override
  void dispose() {
    _hostCtrl.dispose();
    _portCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final host = _hostCtrl.text.trim();
    final port = int.tryParse(_portCtrl.text.trim()) ?? 4000;
    await ApiClient.saveSettings(host, port);
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('서버 주소가 저장됐습니다. 앱을 재시작하면 적용됩니다.')),
      );
    }
  }

  @override
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('설정')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              flex: 6,
              child: _serverCard(),
            ),
            const SizedBox(width: 16),
            Expanded(
              flex: 4,
              child: _infoCard(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _serverCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.dns, size: 22),
                SizedBox(width: 8),
                Text(
                  '서버 연결 설정',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
                ),
              ],
            ),
            const SizedBox(height: 8),
            const Text(
              'GVision 서버의 IP와 포트를 설정합니다. 현장 태블릿이 장비 PC와 같은 네트워크에 연결되어 있어야 합니다.',
              style: TextStyle(fontSize: 12, color: Colors.white54, height: 1.4),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _hostCtrl,
              decoration: const InputDecoration(
                labelText: '서버 IP 주소',
                hintText: '예: 192.168.0.100',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.dns),
              ),
              keyboardType: TextInputType.url,
            ),
            const SizedBox(height: 14),
            TextField(
              controller: _portCtrl,
              decoration: const InputDecoration(
                labelText: '포트',
                hintText: '4000',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.numbers),
              ),
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              height: 46,
              child: FilledButton.icon(
                icon: const Icon(Icons.save),
                label: const Text('저장'),
                onPressed: _save,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _infoCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.info_outline, size: 22),
                SizedBox(width: 8),
                Text(
                  '앱 정보',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
                ),
              ],
            ),
            const SizedBox(height: 18),
            _infoRow('앱 이름', 'GVision Monitor'),
            _infoRow('버전', 'v1.0.0'),
            _infoRow('용도', '현장 장비 상태 모니터링 및 검사 알림 분석'),
            const SizedBox(height: 18),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: Colors.white10,
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Text(
                '설정 변경 후에는 앱을 재시작해야 서버 연결 정보가 완전히 반영됩니다.',
                style: TextStyle(fontSize: 12, color: Colors.white70, height: 1.4),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _infoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 7),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 70,
            child: Text(
              label,
              style: const TextStyle(fontSize: 12, color: Colors.white54),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontSize: 13, color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }
}
