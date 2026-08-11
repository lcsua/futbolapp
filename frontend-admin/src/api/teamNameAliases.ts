import { apiClient } from './apiClient'

export type TeamNameAlias = {
  id: string
  teamId: string
  alias: string
  normalizedAlias: string
}

export const teamNameAliasesService = {
  list: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<{ items: TeamNameAlias[] }>(`/api/leagues/${leagueId}/team-name-aliases`, signal),

  upsert: (
    leagueId: string,
    items: Array<{ teamId: string; alias: string }>,
    signal?: AbortSignal
  ) =>
    apiClient.post<{ upsertedCount: number }>(
      `/api/leagues/${leagueId}/team-name-aliases`,
      { items },
      signal
    ),
}
