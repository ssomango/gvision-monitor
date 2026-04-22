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

router.get('/', async (req, res) => {
  try {
    const status = getLastStatus();

    if (!status) {
      return res.status(503).json({
        online: false,
        message: 'GvisionWpf에 연결할 수 없습니다.',
        connectedClients: wsManager.getClientCount(),
      });
    }

    res.json({
      online: true,
      ...status,
      connectedClients: wsManager.getClientCount(),
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
