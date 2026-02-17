import { apiClient } from './apiClient'
import type { Season, SeasonFormData } from './types'

export const seasonsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Season[]>(`/api/leagues/${leagueId}/seasons`, signal),

  create: (leagueId: string, data: SeasonFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/seasons`, data, signal),

  update: (leagueId: string, seasonId: string, data: SeasonFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/seasons/${seasonId}`, data, signal),
}
