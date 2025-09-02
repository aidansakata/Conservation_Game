const raw = (typeof window !== 'undefined' ? window?.process?.env?.NEXT_PUBLIC_FLAGS : process.env.NEXT_PUBLIC_FLAGS) || '';
const parts = raw.split(',').map((s) => s.trim()).filter(Boolean);
const set = new Set(parts);

export function isFlagOn(name: string): boolean {
	return set.has(name);
}
