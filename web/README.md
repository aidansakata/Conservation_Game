# Web

Next.js + TypeScript + Tailwind.

## Local Dev
1) Install deps with pnpm at repo root: `pnpm i`
2) Create `.env.local` using `.env.example` values
3) (Optional) Start DB/API: `docker compose -f ../infra/docker-compose.yml up -d`
4) Run only web: `pnpm dev:web` (from repo root) or `pnpm dev` to run web+api

- Unity drop-zone: Put your WebGL `Build/` output under `public/unity/Build/`
- API proxy: In development, requests to `/api/*` are proxied to `NEXT_PUBLIC_API_BASE` via `rewrites()`
- Play page: Level picker (1–3) and Unity placeholder if build files are missing
- Flags: `NEXT_PUBLIC_FLAGS=demo` enables a no-op "Show Optimal Solution" button on `/play`
