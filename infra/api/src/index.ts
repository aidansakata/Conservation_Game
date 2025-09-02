import express from 'express';
import cors from 'cors';
import { Client } from 'pg';
import fs from 'fs';
import path from 'path';

const app = express();
const port = Number(process.env.PORT ?? 4000);

app.use(cors({ origin: 'http://localhost:3001' }));
app.use(express.json());

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

app.get('/api/health', (_req, res) => {
	res.json({ ok: true });
});

app.get('/api/leaderboard', async (req, res) => {
	try {
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

app.post('/api/score', async (req, res) => {
	try {
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
		await pgClient.connect();
		await ensureSchema();
	}
	app.listen(port, () => console.log(`[infra-api] listening on http://localhost:${port}`));
}

start().catch((e) => {
	console.error('Fatal error', e);
	process.exit(1);
});
