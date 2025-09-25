export async function fetchCatalog() {
  try {
    const res = await fetch('/levels/catalog.json', { cache: 'no-store' });
    if (!res.ok) return { levels: [] };
    return res.json();
  } catch (e) {
    return { levels: [] };
  }
}
