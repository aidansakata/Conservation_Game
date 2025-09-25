import express, { Request, Response } from 'express';
import cors from 'cors';
import { Client } from 'pg';
import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';
import multer from 'multer';

import { normalizeModelJsonToLevelJsons } from './ingest/normalizeLevel.js';

dotenv.config();

const app = express();
const port = Number(process.env.PORT ?? 4000);

const WEB_ORIGIN = process.env.WEB_ORIGIN ?? 'http://localhost:3001';
const ADMIN_TOKEN = process.env.ADMIN_TOKEN ?? '';

app.use(cors({ origin: WEB_ORIGIN === '*' ? true : WEB_ORIGIN }));
app.use(express.json());

// storage path for uploaded/normalized levels
const STORAGE_LEVELS = path.join(process.cwd(), 'storage', 'levels');
const CATALOG_PATH = path.join(STORAGE_LEVELS, 'catalog.json');

// ensure storage dirs exist
if (!fs.existsSync(STORAGE_LEVELS)) fs.mkdirSync(STORAGE_LEVELS, { recursive: true });
if (!fs.existsSync(CATALOG_PATH)) fs.writeFileSync(CATALOG_PATH, JSON.stringify({ levels: [] }, null, 2));

// serve normalized levels under /levels
app.use('/levels', express.static(STORAGE_LEVELS, { maxAge: '1m', etag: true }));

// multer for file uploads (limit 2MB)
const upload = multer({ storage: multer.memoryStorage(), limits: { fileSize: 2 * 1024 * 1024 } });

// --- Optional DB wiring for leaderboard ---
const databaseUrl = process.env.DATABASE_URL;
if (!databaseUrl) {
  console.warn('[infra-api] DATABASE_URL not set; API will start but DB calls will fail');
}
const pgClient = new Client({ connectionString: databaseUrl });

async function ensureSchema() {
  const { rows } = await pgClient.query("select to_regclass('public.scores') as exists");
  const exists = rows?.[0]?.exists !== null;
  if (!exists) {
    const sqlPath = path.join(process.cwd(), 'schema.sql');
    if (fs.existsSync(sqlPath)) {
      const sql = fs.readFileSync(sqlPath, 'utf8');
      await pgClient.query(sql);
    } else {
      await pgClient.query(
        `CREATE TABLE IF NOT EXISTS scores (
          id bigserial primary key,
          player_name text not null,
          level int not null,
          score int not null,
          percent_of_optimal numeric(5,2) not null,
          budget int not null,
          created_at timestamptz default now(),
          meta jsonb
        );`
      );
    }
  }
}

// --- Health ---
app.get('/api/health', (_req: Request, res: Response) => {
  res.json({ ok: true });
});

// --- Admin upload endpoint ---
app.post('/admin/levels/upload', upload.single('file'), async (req: Request, res: Response) => {
  try {
    const token = req.header('x-admin-token') ?? '';
    if (!ADMIN_TOKEN || token !== ADMIN_TOKEN) return res.status(401).json({ error: 'unauthorized' });

    if (!req.file) return res.status(400).json({ error: 'missing_file' });
    if (req.file.mimetype !== 'application/json' && !req.file.originalname.toLowerCase().endsWith('.json')) {
      return res.status(400).json({ error: 'invalid_content_type' });
    }

    const text = req.file.buffer.toString('utf8');
    let model: any;
    try {
      model = JSON.parse(text);
    } catch {
      return res.status(400).json({ error: 'invalid_json' });
    }

    const entries = normalizeModelJsonToLevelJsons(model);
    if (!entries || entries.length === 0) return res.status(400).json({ error: 'no_levels_found' });

    const written: string[] = [];
    for (const e of entries) {
      const outPath = path.join(STORAGE_LEVELS, `${e.key}.json`);
      const tmp = outPath + '.tmp';
      fs.writeFileSync(tmp, JSON.stringify(e.level, null, 2), 'utf8');
      fs.renameSync(tmp, outPath);
      written.push(`/levels/${e.key}.json`);
    }

    // rebuild catalog atomically by scanning storage folder for all .json files
    const files = fs
      .readdirSync(STORAGE_LEVELS)
      .filter((f) => f.toLowerCase().endsWith('.json') && f !== 'catalog.json');

    const allEntries: { id: string; width: number; height: number; budget: number; path: string }[] = [];
    for (const f of files) {
      try {
        const txt = fs.readFileSync(path.join(STORAGE_LEVELS, f), 'utf8');
        const parsed = JSON.parse(txt);
        const id = f.replace(/\.json$/i, '');
        allEntries.push({
          id,
          width: Number(parsed.width ?? 0),
          height: Number(parsed.height ?? 0),
          budget: Number(parsed.budget ?? 0),
          path: `/levels/${id}.json`,
        });
      } catch {
        // ignore malformed files
      }
    }

    const catalog = { levels: allEntries };
    const tmpCat = CATALOG_PATH + '.tmp';
    fs.writeFileSync(tmpCat, JSON.stringify(catalog, null, 2), 'utf8');
    fs.renameSync(tmpCat, CATALOG_PATH);

    res.json({ ok: true, count: written.length, files: written });
  } catch (e: any) {
    console.error(e);
    res.status(500).json({ error: 'upload_failed', detail: String(e?.message ?? e) });
  }
});

// --- Serve the catalog ---
app.get('/levels/catalog.json', (_req: Request, res: Response) => {
  try {
    if (!fs.existsSync(CATALOG_PATH)) return res.json({ levels: [] });
    const txt = fs.readFileSync(CATALOG_PATH, 'utf8');
    res.setHeader('Cache-Control', 'no-cache');
    res.type('application/json').send(txt);
  } catch (e) {
    console.error(e);
    res.json({ levels: [] });
  }
});

// --- Leaderboard (optional) ---
app.get('/api/leaderboard', async (req: Request, res: Response) => {
  try {
    if (!databaseUrl) return res.status(503).json({ error: 'db_unavailable' });
    const level = Number(req.query.level ?? 1);
    const limit = Math.min(Number(req.query.limit ?? 100), 1000);
    const { rows } = await pgClient.query(
      'SELECT id, player_name, level, score, percent_of_optimal, budget, created_at FROM scores WHERE level = $1 ORDER BY score DESC, created_at ASC LIMIT $2',
      [level, limit]
    );
    res.json(rows);
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: 'failed_to_fetch' });
  }
});

app.post('/api/score', async (req: Request, res: Response) => {
  try {
    if (!databaseUrl) return res.status(503).json({ error: 'db_unavailable' });
    const { player_name, level, score, percent_of_optimal, budget, meta } = req.body ?? {};
    if (!player_name || !Number.isFinite(level) || !Number.isFinite(score) || !Number.isFinite(budget)) {
      return res.status(400).json({ error: 'invalid_payload' });
    }
    const perc = Number(percent_of_optimal);
    if (!Number.isFinite(perc)) {
      return res.status(400).json({ error: 'invalid_percent' });
    }
    const { rows } = await pgClient.query(
      'INSERT INTO scores (player_name, level, score, percent_of_optimal, budget, meta) VALUES ($1,$2,$3,$4,$5,$6) RETURNING id',
      [player_name, level, score, perc, budget, meta ?? null]
    );
    res.status(201).json({ id: rows[0].id });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: 'failed_to_insert' });
  }
});

async function start() {
  if (databaseUrl) {
    try {
      await pgClient.connect();
      await ensureSchema();
    } catch (e) {
      console.error('[infra-api] failed to connect/init db:', e);
    }
  }
  app.listen(port, () => console.log(`[infra-api] listening on http://localhost:${port}`));
}

start().catch((e) => {
  console.error('Fatal error', e);
  process.exit(1);
});
