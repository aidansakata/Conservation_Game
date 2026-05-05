export interface LevelJson {
  schemaVersion: number;
  width: number;
  height: number;
  budget: number;
  tileTypes: string[];
  costData: number[];   // defaulted to 1s
  utilities: number[];   // from model.utilities
  optimalData?: number[] | null; // from model.optimal (0/1 per cell)
  optUtil?: number | null;       // from model.opt_util (carried through; Unity can ignore)
}

export interface CatalogEntry { id: string; width: number; height: number; budget: number; path: string }
export interface Catalog { levels: CatalogEntry[] }

const asInt = (v: any) => (Number.isFinite(v) ? Number(v) : 0);

export function normalizeModelJsonToLevelJsons(model: any): { key: string; level: LevelJson }[] {
  if (!model || typeof model !== 'object') throw new Error('invalid_model');

  const out: { key: string; level: LevelJson }[] = [];

  for (const key of Object.keys(model)) {
    const block = model[key];
    if (!block || typeof block !== 'object') continue;

    // Only read the whitelisted fields from the model JSON
    const types = block.types ?? {};
    const utilities = block.utilities ?? {};
    const optimal = block.optimal ?? {};
    const budget = asInt(block.budget ?? 0);
    const optUtil = block.opt_util != null ? asInt(block.opt_util) : null;

    // infer grid size from the largest numeric key in "types"
    let maxKey = 0;
    for (const k of Object.keys(types)) {
      const ik = Number(k);
      if (Number.isFinite(ik) && ik > maxKey) maxKey = ik;
    }
    if (maxKey <= 0) throw new Error(`no_numeric_keys_for_${key}`);
    const n = Math.sqrt(maxKey);
    if (!Number.isInteger(n)) throw new Error(`invalid_dimensions_for_${key}: maxKey ${maxKey} not a perfect square`);
    const size = n * n;

    // allocate arrays
    const tileTypes = new Array<string>(size).fill('');
    const costData = new Array<number>(size).fill(1);
    const ecoData1 = new Array<number>(size).fill(0);
    const optimalData = new Array<number>(size).fill(0);

    // populate from 1-based keys -> index (k-1)
    for (let i = 1; i <= size; i++) {
      const idx = i - 1;

      const t = types[String(i)];
      if (t != null) {
        tileTypes[idx] = String(t).trim().toLowerCase();
      }

      const u = utilities[String(i)];
      if (u != null) ecoData1[idx] = asInt(u);

      const o = optimal[String(i)];
      if (o != null) optimalData[idx] = o ? 1 : 0;
    }

    const level: LevelJson = {
      schemaVersion: 1,
      width: n,
      height: n,
      budget,
      tileTypes,
      costData,
      utilities: ecoData1,
      optimalData,    // keep, even if zeroes (Unity may ignore)
      optUtil,        // carried as metadata; safe to ignore in Unity
    };

    out.push({ key, level });
  }

  return out;
}

export function buildCatalog(entries: { key: string; level: LevelJson }[]): Catalog {
  const levels = entries.map((e) => ({
    id: e.key,
    width: e.level.width,
    height: e.level.height,
    budget: e.level.budget,
    path: `/levels/${e.key}.json`,
  }));
  return { levels };
}
