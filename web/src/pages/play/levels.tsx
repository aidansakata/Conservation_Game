import { useEffect, useState } from 'react';
import { fetchCatalog } from '../../utils/levels';
import { useRouter } from 'next/router';

export default function PlayLevels() {
  const [catalog, setCatalog] = useState<{ levels: any[] }>({ levels: [] });
  const router = useRouter();

  useEffect(() => {
    (async () => {
      const c = await fetchCatalog();
      setCatalog(c);
    })();
  }, []);

  function startLevel(id: string) {
    // store for Unity bridge to pickup
    try { localStorage.setItem('unity:selectedLevelId', id); } catch {}
    router.push('/play');
  }

  return (
    <div style={{ padding: 20 }}>
      <h1>Available Levels</h1>
      <ul>
        {catalog.levels.map((l: any) => (
          <li key={l.id} style={{ marginBottom: 8 }}>
            <strong>{l.id}</strong> — {l.width}x{l.height} budget={l.budget}
            <button style={{ marginLeft: 8 }} onClick={() => startLevel(l.id)}>Play</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
