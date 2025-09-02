import { useEffect, useState } from 'react';
import Link from 'next/link';
import UnityCanvas from '@/src/components/UnityCanvas';
import { tokens } from '@/src/theme/tokens';
import { isFlagOn } from '@/src/lib/flags';

export default function PlayPage() {
	const [level, setLevel] = useState<number>(1);
	const [missingUnity, setMissingUnity] = useState<boolean>(false);

	useEffect(() => {
		async function check() {
			try {
				const res = await fetch('/unity/Build/Build.loader.js', { method: 'HEAD' });
				setMissingUnity(!res.ok);
			} catch {
				setMissingUnity(true);
			}
		}
		check();
	}, []);

	return (
		<div style={{ padding: tokens.spacing.lg, color: tokens.colors.foreground }}>
			{missingUnity && (
				<div style={{
					background: '#fff3cd', color: '#664d03', border: '1px solid #ffecb5',
					padding: tokens.spacing.md, borderRadius: tokens.radii.md, marginBottom: tokens.spacing.md,
				}}>
					<strong>Drop your Unity WebGL build into /web/public/unity/Build to play. (See README)</strong>
				</div>
			)}

			<h1 style={{ marginBottom: tokens.spacing.md }}>Play</h1>

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

				{isFlagOn('demo') && (
					<button style={{ marginLeft: tokens.spacing.md, padding: `${tokens.spacing.xs} ${tokens.spacing.sm}` }}>
						Show Optimal Solution
					</button>
				)}
			</div>

			<div style={{ display: 'flex', gap: tokens.spacing.sm, marginBottom: tokens.spacing.md }}>
				<Link href="/how-to">How To</Link>
				<Link href="/about">About</Link>
			</div>

			<UnityCanvas selectedLevel={level} />
		</div>
	);
}
