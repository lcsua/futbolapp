import { apiClient } from './apiClient'
import type { League, LeagueFormData } from './types'

export const leaguesService = {
  getMyLeagues: (signal?: AbortSignal) =>
    apiClient.get<League[]>('/api/leagues', signal),

  getById: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<League>(`/api/leagues/${leagueId}`, signal),

  create: (data: LeagueFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>('/api/leagues', data, signal),

  update: (leagueId: string, data: LeagueFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}`, data, signal),
}
