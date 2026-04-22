/**
 * routes/inspections.js — 시계열 검사 데이터 API
 *
 * GET /api/inspections/series
 *   원시 검사 결과 목록 (모든 필드)
 *   쿼리: from, to (ISO string), lotId
 *
 * GET /api/inspections/yield
 *   수율 시계열 — 1분 단위 집계
 *   그래프 X축: minute, Y축: yield(%)
 *   쿼리: from, to, lotId
 *
 * GET /api/inspections/duration
 *   처리시간 시계열 — 1분 단위 평균
 *   쿼리: from, to, lotId
 *
 * GET /api/inspections/errors
 *   불량 유형별 건수 (파이/바 차트용)
 *   쿼리: lotId (필수)
 *
 * GET /api/inspections/heatmap
 *   XY 위치별 불량 분포 (히트맵용)
 *   쿼리: lotId (필수)
 */

const express = require('express');
const router = express.Router();
const db = require('../db');

// 기본 시간 범위: 현재로부터 1시간 전 ~ 현재
function defaultRange() {
  const to   = new Date().toISOString();
  const from = new Date(Date.now() - 60 * 60 * 1000).toISOString();
  return { from, to };
}

// GET /api/inspections/series
router.get('/series', (req, res) => {
  const { from, to } = req.query.from ? req.query : defaultRange();
  const lotId = req.query.lotId ? parseInt(req.query.lotId) : null;

  const data = db.getInspectionSeries(from, to, lotId);
  res.json({ count: data.length, from, to, data });
});

// GET /api/inspections/yield
router.get('/yield', (req, res) => {
  const { from, to } = req.query.from ? req.query : defaultRange();
  const lotId = req.query.lotId ? parseInt(req.query.lotId) : null;

  const data = db.getYieldSeries(from, to, lotId);
  res.json({ from, to, data });
});

// GET /api/inspections/duration
router.get('/duration', (req, res) => {
  const { from, to } = req.query.from ? req.query : defaultRange();
  const lotId = req.query.lotId ? parseInt(req.query.lotId) : null;

  const data = db.getDurationSeries(from, to, lotId);
  res.json({ from, to, data });
});

// GET /api/inspections/errors
router.get('/errors', (req, res) => {
  const lotId = parseInt(req.query.lotId);
  if (!lotId) return res.status(400).json({ error: 'lotId 파라미터가 필요합니다.' });

  const data = db.getErrorBreakdown(lotId);
  res.json({ lotId, data });
});

// GET /api/inspections/heatmap
router.get('/heatmap', (req, res) => {
  const lotId = parseInt(req.query.lotId);
  if (!lotId) return res.status(400).json({ error: 'lotId 파라미터가 필요합니다.' });

  const data = db.getFailHeatmap(lotId);
  res.json({ lotId, data });
});

module.exports = router;
