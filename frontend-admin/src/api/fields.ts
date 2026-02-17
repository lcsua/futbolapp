import { apiClient } from './apiClient'
import type { Field, FieldFormData } from './types'

export const fieldsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Field[]>(`/api/leagues/${leagueId}/fields`, signal),

  create: (leagueId: string, data: FieldFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/fields`, data, signal),

  update: (leagueId: string, fieldId: string, data: FieldFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/fields/${fieldId}`, data, signal),
}
