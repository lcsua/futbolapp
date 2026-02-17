# Football Admin (frontend-admin)

Mobile-first React admin UI for the multi-league football management system.

## Stack

- React 19, TypeScript, Vite
- Material UI (MUI)
- React Router, React Query
- Centralized `apiClient` for all REST calls

## Setup

```bash
npm install
```

## Run

```bash
npm run dev
```

Runs at [http://localhost:5173](http://localhost:5173). API requests to `/api/*` are proxied to **https://localhost:7272** (backend). Start the backend before the frontend.

To use another API URL, set `VITE_API_BASE_URL` in `.env` (e.g. `VITE_API_BASE_URL=https://localhost:7272`).

## Features

- **Layout**: Top app bar with title and league selector; mobile drawer menu.
- **Leagues list**: Lists the current user’s leagues (GET `/api/leagues`). Tap a card to go to league detail (placeholder).
- **League selector**: Dropdown in the top bar; selection is stored in context for future league-scoped pages (Seasons, Teams).

## Project structure

- `src/api/` — `apiClient` and API modules (e.g. `leagues.ts`)
- `src/components/` — `AppLayout`, `LeagueSelector`, `Sidebar`
- `src/contexts/` — `LeagueContext` (selected league)
- `src/pages/` — `LeaguesListPage`, placeholders
- `src/theme.ts` — MUI theme
