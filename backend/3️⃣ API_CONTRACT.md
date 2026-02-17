# API Contract

## Leagues
GET /api/leagues
GET /api/leagues/{leagueId}
POST /api/leagues

## Seasons
GET /api/leagues/{leagueId}/seasons
POST /api/leagues/{leagueId}/seasons

## Rules
- All endpoints are league-scoped
- userId is resolved from authentication (future)
- Errors are returned using consistent format
