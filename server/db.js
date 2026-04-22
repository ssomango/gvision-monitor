/**
 * db.js — SQLite 읽기 전담 모듈
 *
 * better-sqlite3는 동기(sync) 방식입니다.
 * GvisionWpf가 이미 DB에 쓰고 있으므로, 우리는 읽기(READ ONLY)만 합니다.
 *
 * GvisionWpf DB 테이블:
 *   histories          - 시스템/LOT/Recipe 이벤트 로그
 *   inspection_results - 매 검사 결과 (시계열 핵심 데이터)
 *   lot                - Lot 목록
 */

const Database = require('better-sqlite3');
const config = require('./config');
const fs = require('fs');

let db = null;

/**
 * DB 연결을 가져옵니다.
 * DB 파일이 없으면 null을 반환합니다 (GvisionWpf가 아직 실행 안 된 경우).
 */
function getDb() {
  if (db) return db;

  if (!fs.existsSync(config.DB_PATH)) {
    console.warn(`[DB] 파일 없음: ${config.DB_PATH}`);
    console.warn('[DB] GvisionWpf가 실행된 적 있는지 확인하세요.');
    return null;
  }

  // readonly: true → 읽기 전용 (GvisionWpf 쓰기와 충돌 방지)
  db = new Database(config.DB_PATH, { readonly: true });
  console.log(`[DB] 연결됨: ${config.DB_PATH}`);
  return db;
}

// ─────────────────────────────────────────────
// histories 테이블 쿼리
// ─────────────────────────────────────────────

/**
 * 최근 이벤트 목록 조회
 * @param {number} limit - 가져올 개수 (기본 50)
 * @param {string|null} logType - 필터 (SystemLogs/LOTLogs/RecipeLogs 등)
 */
function getRecentEvents(limit = 50, logType = null) {
  const db = getDb();
  if (!db) return [];

  let sql = `
    SELECT Id, Time, Package, LotId, Camera, Inspection, LogType, Description, ImagePath
    FROM histories
  `;
  const params = [];

  if (logType) {
    sql += ` WHERE LogType = ?`;
    params.push(logType);
  }

  sql += ` ORDER BY Id DESC LIMIT ?`;
  params.push(limit);

  return db.prepare(sql).all(...params);
}

/**
 * 특정 이벤트 전후 N분의 이벤트 목록 (맥락 조회)
 * @param {string} time - 이벤트 발생 시각 (ISO string)
 * @param {number} beforeMin - 이전 몇 분
 * @param {number} afterMin - 이후 몇 분
 */
function getEventContext(time, beforeMin = 5, afterMin = 5) {
  const db = getDb();
  if (!db) return [];

  return db.prepare(`
    SELECT Id, Time, Package, LotId, Camera, Inspection, LogType, Description
    FROM histories
    WHERE Time BETWEEN datetime(?, '-${beforeMin} minutes')
                   AND datetime(?, '+${afterMin} minutes')
    ORDER BY Time ASC
  `).all(time, time);
}

// ─────────────────────────────────────────────
// inspection_results 테이블 쿼리 (시계열 핵심)
// ─────────────────────────────────────────────

/**
 * 시계열 검사 결과 조회 (그래프용)
 * @param {string} from - 시작 시각 (ISO string)
 * @param {string} to   - 종료 시각 (ISO string)
 * @param {number|null} lotId - Lot 필터
 */
function getInspectionSeries(from, to, lotId = null) {
  const db = getDb();
  if (!db) return [];

  let sql = `
    SELECT Id, LotId, RecipeName, Duration, StartTime, EndTime,
           Item, XPos, YPos, XOffset, YOffset, TOffset,
           PackageWidth, PackageHeight, HasDevice, InspectionType
    FROM inspection_results
    WHERE StartTime BETWEEN ? AND ?
  `;
  const params = [from, to];

  if (lotId !== null) {
    sql += ` AND LotId = ?`;
    params.push(lotId);
  }

  sql += ` ORDER BY StartTime ASC`;

  return db.prepare(sql).all(...params);
}

/**
 * 수율(Yield) 시계열 — 1분 단위로 집계
 * pass 건수 / 전체 건수 = 수율
 */
function getYieldSeries(from, to, lotId = null) {
  const db = getDb();
  if (!db) return [];

  let sql = `
    SELECT
      strftime('%Y-%m-%dT%H:%M:00', StartTime) AS minute,
      COUNT(*) AS total,
      SUM(CASE WHEN Item = 'PASS' THEN 1 ELSE 0 END) AS pass,
      ROUND(SUM(CASE WHEN Item = 'PASS' THEN 1.0 ELSE 0 END) / COUNT(*) * 100, 2) AS yield
    FROM inspection_results
    WHERE StartTime BETWEEN ? AND ?
  `;
  const params = [from, to];

  if (lotId !== null) {
    sql += ` AND LotId = ?`;
    params.push(lotId);
  }

  sql += ` GROUP BY minute ORDER BY minute ASC`;

  return db.prepare(sql).all(...params);
}

/**
 * 평균 처리시간(Duration) 시계열 — 1분 단위 집계
 */
function getDurationSeries(from, to, lotId = null) {
  const db = getDb();
  if (!db) return [];

  let sql = `
    SELECT
      strftime('%Y-%m-%dT%H:%M:00', StartTime) AS minute,
      ROUND(AVG(Duration), 1) AS avgDuration,
      MAX(Duration) AS maxDuration
    FROM inspection_results
    WHERE StartTime BETWEEN ? AND ?
  `;
  const params = [from, to];

  if (lotId !== null) {
    sql += ` AND LotId = ?`;
    params.push(lotId);
  }

  sql += ` GROUP BY minute ORDER BY minute ASC`;

  return db.prepare(sql).all(...params);
}

/**
 * 불량 유형별 건수 (파이/바 차트용)
 */
function getErrorBreakdown(lotId) {
  const db = getDb();
  if (!db) return [];

  return db.prepare(`
    SELECT Item AS errorType, COUNT(*) AS count
    FROM inspection_results
    WHERE LotId = ? AND Item != 'PASS'
    GROUP BY Item
    ORDER BY count DESC
  `).all(lotId);
}

/**
 * XY 위치별 불량 수 (히트맵용)
 */
function getFailHeatmap(lotId) {
  const db = getDb();
  if (!db) return [];

  return db.prepare(`
    SELECT XPos, YPos, COUNT(*) AS failCount
    FROM inspection_results
    WHERE LotId = ? AND Item != 'PASS'
    GROUP BY XPos, YPos
  `).all(lotId);
}

// ─────────────────────────────────────────────
// lot 테이블 쿼리
// ─────────────────────────────────────────────

/**
 * Lot 목록 조회
 */
function getLots(limit = 20) {
  const db = getDb();
  if (!db) return [];

  return db.prepare(`
    SELECT Id, Package, LotNumber, StartTime, EndTime
    FROM lot
    ORDER BY Id DESC
    LIMIT ?
  `).all(limit);
}

/**
 * 특정 Lot 상세 + 수율 통계
 */
function getLotStats(lotId) {
  const db = getDb();
  if (!db) return null;

  const lot = db.prepare(`
    SELECT Id, Package, LotNumber, StartTime, EndTime FROM lot WHERE Id = ?
  `).get(lotId);

  if (!lot) return null;

  const stats = db.prepare(`
    SELECT
      InspectionType,
      COUNT(*) AS total,
      SUM(CASE WHEN Item = 'PASS' THEN 1 ELSE 0 END) AS pass,
      ROUND(SUM(CASE WHEN Item = 'PASS' THEN 1.0 ELSE 0 END) / COUNT(*) * 100, 2) AS yield
    FROM inspection_results
    WHERE LotId = ?
    GROUP BY InspectionType
  `).all(lotId);

  return { ...lot, stats };
}

/**
 * histories 테이블의 마지막 Id를 반환 (새 이벤트 감지용)
 */
function getLatestHistoryId() {
  const db = getDb();
  if (!db) return 0;

  const row = db.prepare(`SELECT MAX(Id) AS maxId FROM histories`).get();
  return row?.maxId ?? 0;
}

module.exports = {
  getRecentEvents,
  getEventContext,
  getInspectionSeries,
  getYieldSeries,
  getDurationSeries,
  getErrorBreakdown,
  getFailHeatmap,
  getLots,
  getLotStats,
  getLatestHistoryId,
};
