/* eslint-disable @next/next/no-sync-scripts */
'use client';

import { useEffect, useMemo, useRef, useState } from 'react';

declare global {
	interface Window {
		createUnityInstance?: (
			canvas: HTMLCanvasElement,
			config: Record<string, unknown>
		) => Promise<{ SendMessage: (obj: string, method: string, param?: unknown) => void }>;
		UnityBridge?: { startLevel: (n: number) => void };
	}
}

export type UnityCanvasProps = { selectedLevel?: number };

export default function UnityCanvas({ selectedLevel = 1 }: UnityCanvasProps) {
	const canvasRef = useRef<HTMLCanvasElement | null>(null);
	const [unityReady, setUnityReady] = useState(false);
	const [missing, setMissing] = useState(false);
	const [error, setError] = useState<string | null>(null);

	const buildBase = '/unity/Build';

	const files = useMemo(() => ({
		loader: `${buildBase}/Build.loader.js`,
		data: `${buildBase}/Build.data` as const,
		framework: `${buildBase}/Build.framework.js` as const,
		code: `${buildBase}/Build.wasm` as const,
	}), []);

	useEffect(() => {
		async function init() {
			try {
				const res = await fetch(files.loader, { method: 'HEAD' });
				if (!res.ok) {
					setMissing(true);
					return;
				}

				const script = document.createElement('script');
				script.src = files.loader;
				script.onload = async () => {
					try {
						if (!window.createUnityInstance || !canvasRef.current) {
							throw new Error('Unity loader not initialized');
						}
						const instance = await window.createUnityInstance(canvasRef.current, {
							dataUrl: files.data,
							frameworkUrl: files.framework,
							codeUrl: files.code,
						});
						setUnityReady(true);

						window.UnityBridge = {
							startLevel: (n: number) => instance.SendMessage('GameManager', 'OnLevelSelected', n),
						};

						// Backwards-compatible numeric selection
						instance.SendMessage('GameManager', 'OnLevelSelected', selectedLevel ?? 1);

						// If a string id was selected in the web UI, pass it to GridManager.StartLevelById
						try {
							const id = localStorage.getItem('unity:selectedLevelId');
							if (id && id.length > 0) {
								instance.SendMessage('GridManager', 'StartLevelById', id);
								localStorage.removeItem('unity:selectedLevelId');
							}
						} catch { }
					} catch (e: unknown) {
						setError((e as Error).message);
					}
				};
				script.onerror = () => setError('Failed to load Unity loader script');
				document.body.appendChild(script);
			} catch (e: unknown) {
				setError((e as Error).message);
			}
		}
		init();
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	useEffect(() => {
		if (unityReady && window.UnityBridge) {
			window.UnityBridge.startLevel(selectedLevel ?? 1);
		}
	}, [unityReady, selectedLevel]);

	if (missing) {
		return (
			<div style={{ padding: '1rem', border: '1px dashed #888', borderRadius: 8 }}>
				<strong>Unity build not found.</strong>
				<div>Drop your Unity WebGL build into /web/public/unity/Build to play. (See README)</div>
			</div>
		);
	}

	if (error) {
		return <div style={{ color: 'red' }}>Error: {error}</div>;
	}

	return (
		<canvas ref={canvasRef} style={{ width: '100%', height: 600, background: '#000' }} />
	);
}
