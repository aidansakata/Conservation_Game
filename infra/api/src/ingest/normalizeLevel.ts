export interface LevelJson {
  schemaVersion: number;
  width: number;
  height: number;
  budget: number;
  tileTypes: number[];
  costData: number[];
  ecoData1: number[];
  ecoData2?: number[] | null;
  ecoData3?: number[] | null;
  lockedData?: number[] | null;
  displayValues?: number[] | null;
  optimalData?: number[] | null;
}

export interface CatalogEntry { id: string; width: number; height: number; budget: number; path: string }
export interface Catalog { levels: CatalogEntry[] }

const TYPE_MAP: Record<string, number> = {
  habitat: 0,
  city: 1,
  forest: 2,
  grassland: 3,
  farmland: 4,
  water: 5,
  road: 6,
};

function asInt(v: any) { return Number.isFinite(v) ? Number(v) : 0; }

export function normalizeModelJsonToLevelJsons(model: any): { key: string; level: LevelJson }[] {
  if (!model || typeof model !== 'object') throw new Error('invalid_model');

  const out: { key: string; level: LevelJson }[] = [];

  for (const key of Object.keys(model)) {
    const block = model[key];
    if (!block || typeof block !== 'object') continue;

    const types = block.types ?? {};
    const utilities = block.utilities ?? {};
    const optimal = block.optimal ?? {};
    const budget = asInt(block.budget ?? 0);

    // find max numeric key in types
    let maxKey = 0;
    for (const k of Object.keys(types)) {
      const ik = Number(k);
      if (Number.isFinite(ik) && ik > maxKey) maxKey = ik;
    }
    if (maxKey <= 0) throw new Error(`no_numeric_keys_for_${key}`);
    const n = Math.sqrt(maxKey);
    if (!Number.isInteger(n)) throw new Error(`invalid_dimensions_for_${key}: maxKey ${maxKey} not a perfect square`);
    const size = n * n;

    // Build arrays length size; fill defaults
    const tileTypes = new Array<number>(size).fill(0);
    const costData = new Array<number>(size).fill(1);
    const ecoData1 = new Array<number>(size).fill(0);
    const optimalData = new Array<number>(size).fill(0);

    // Populate from 1-based keys -> index k-1
    for (let i = 1; i <= size; i++) {
      const idx = i - 1;
      const t = types[String(i)];
      if (t != null) {
        const ti = typeof t === 'string' ? (TYPE_MAP[t] ?? 0) : Number(t);
        tileTypes[idx] = Number.isFinite(ti) ? ti : 0;
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
      budget: budget,
      tileTypes,
      costData,
      ecoData1,
      ecoData2: null,
      ecoData3: null,
      lockedData: null,
      displayValues: null,
      optimalData,
    };

    out.push({ key, level });
  }

  return out;
}

export function buildCatalog(entries: { key: string; level: LevelJson }[]): Catalog {
  const levels = entries.map((e) => ({ id: e.key, width: e.level.width, height: e.level.height, budget: e.level.budget, path: `/levels/${e.key}.json` }));
  return { levels };
}
