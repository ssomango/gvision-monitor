/**
 * routes/status.js
 *
 * GET /api/status
 *   → GvisionWpf /api/status를 그대로 중계 + 서버 연결 클라이언트 수 추가
 */

const express = require('express');
const router = express.Router();
const axios = require('axios');
const config = require('../config');
const wsManager = require('../websocket');
const { getLastStatus } = require('../poller');

// GvisionWpf 오프라인일 때 Grafana 01번 대시보드에 보여줄 mock 데이터
const MOCK_STATUS = {
  online: false,
  runningMode: 'OFFLINE',
  recipeName: '(GvisionWpf 미실행)',
  lotNo: '-',
  connectedClients: 0,
};

router.get('/', async (req, res) => {
  try {
    const status = getLastStatus();
    const data = status
      ? { online: true, ...status, connectedClients: wsManager.getClientCount() }
      : { ...MOCK_STATUS, connectedClients: wsManager.getClientCount() };

    // Grafana Infinity 플러그인은 배열 형태를 선호
    res.json([data]);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
