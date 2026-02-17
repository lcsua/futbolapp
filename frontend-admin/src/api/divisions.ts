import { apiClient } from './apiClient'
import type { Division, DivisionFormData } from './types'

export const divisionsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Division[]>(`/api/leagues/${leagueId}/divisions`, signal),

  create: (leagueId: string, data: DivisionFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/divisions`, data, signal),

  update: (leagueId: string, divisionId: string, data: DivisionFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/divisions/${divisionId}`, data, signal),
}
