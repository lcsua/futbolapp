import { apiClient } from './apiClient'
import type { FieldBlackoutItem, FieldBlackoutFormData } from './types'

export interface FieldBlackoutsResponse {
  items: FieldBlackoutItem[]
}

export const fieldBlackoutsService = {
  get: (leagueId: string, fieldId: string, signal?: AbortSignal) =>
    apiClient.get<FieldBlackoutsResponse>(`/api/leagues/${leagueId}/fields/${fieldId}/blackouts`, signal),

  create: (leagueId: string, fieldId: string, data: FieldBlackoutFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/fields/${fieldId}/blackouts`, data, signal),

  delete: (leagueId: string, fieldId: string, blackoutId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/fields/${fieldId}/blackouts/${blackoutId}`, signal),
}
