import { useEffect, useState } from 'react';
import { tokens } from '@/src/theme/tokens';

type ScoreRow = {
	id: number;
	player_name: string;
	level: number;
	score: number;
	percent_of_optimal: string;
	budget: number;
	created_at: string;
};

export default function HallOfFamePage() {
	const [level, setLevel] = useState<number>(1);
	const [rows, setRows] = useState<ScoreRow[]>([]);
	const [loading, setLoading] = useState<boolean>(true);

	useEffect(() => {
		let cancelled = false;
		async function load() {
			setLoading(true);
			try {
				const res = await fetch(`/api/leaderboard?level=${level}`);
				const data: ScoreRow[] = await res.json();
				if (!cancelled) setRows(data);
			} catch {
				if (!cancelled) setRows([]);
			} finally {
				if (!cancelled) setLoading(false);
			}
		}
		load();
		return () => { cancelled = true; };
	}, [level]);

	return (
		<div style={{ padding: tokens.spacing.lg, color: tokens.colors.foreground }}>
			<h1 style={{ marginBottom: tokens.spacing.md }}>Hall of Fame</h1>
			<div style={{ display: 'flex', gap: tokens.spacing.sm, alignItems: 'center', marginBottom: tokens.spacing.md }}>
				<span>Level:</span>
				{[1, 2, 3].map((n) => (
					<button key={n} onClick={() => setLevel(n)} style={{
						padding: `${tokens.spacing.xs} ${tokens.spacing.sm}`,
						borderRadius: tokens.radii.sm,
						border: '1px solid ' + tokens.colors.border,
						background: n === level ? tokens.colors.primary : tokens.colors.card,
						color: n === level ? tokens.colors.primaryForeground : tokens.colors.cardForeground,
					}}>
						{n}
					</button>
				))}
			</div>

			{loading ? (
				<div>Loading…</div>
			) : rows.length === 0 ? (
				<div>No scores yet.</div>
			) : (
				<table style={{ width: '100%', borderCollapse: 'collapse' }}>
					<thead>
						<tr>
							<th align="left">Player</th>
							<th align="right">Score</th>
							<th align="right">% Optimal</th>
							<th align="right">Budget</th>
							<th align="left">When</th>
						</tr>
					</thead>
					<tbody>
						{rows.map((r) => (
							<tr key={r.id}>
								<td>{r.player_name}</td>
								<td align="right">{r.score}</td>
								<td align="right">{r.percent_of_optimal}</td>
								<td align="right">{r.budget}</td>
								<td>{new Date(r.created_at).toLocaleString()}</td>
							</tr>
						))}
					</tbody>
				</table>
			)}
		</div>
	);
}
