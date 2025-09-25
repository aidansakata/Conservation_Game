import { useState } from 'react';

export default function AdminLevelsPage() {
  const [token, setToken] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<string | null>(null);

  async function upload() {
    if (!file) return setResult('No file selected');
    const form = new FormData();
    form.append('file', file, file.name);
    try {
      const res = await fetch('/admin/levels/upload', { method: 'POST', headers: { 'x-admin-token': token }, body: form });
      const json = await res.json();
      setResult(JSON.stringify(json, null, 2));
    } catch (e) {
      setResult(String(e));
    }
  }

  return (
    <div style={{ padding: 20 }}>
      <h1>Admin: Upload Levels</h1>
      <div style={{ marginBottom: 10 }}>
        <label>Admin Token: </label>
        <input value={token} onChange={(e) => setToken(e.target.value)} style={{ width: 400 }} />
      </div>
      <div style={{ marginBottom: 10 }}>
        <input type="file" accept="application/json" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
      </div>
      <div>
        <button onClick={upload}>Upload</button>
      </div>
      {result && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{result}</pre>}
    </div>
  );
}
