## Conservation Monorepo

This repository contains:
- `web`: Next.js + TypeScript + Tailwind web app
- `infra`: Docker Compose (Postgres) + minimal API skeleton
- `unity`: Placeholder for Unity project notes (actual Unity project is at repository root)

### Prerequisites
- Node.js 18+
- Docker + Docker Compose

### Quick start
- Web: `cd web && npm install && npm run dev`
- Infra (DB): `cd infra && docker compose up -d`
- API: `cd infra/api && npm install && npm start`

### CI
- GitHub Actions builds the web app on push/PR
- Unity workflow caches the `Library/` folder but skips building for now




