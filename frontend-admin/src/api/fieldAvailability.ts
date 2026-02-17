import { apiClient } from './apiClient'
import type { FieldAvailabilityItem, FieldAvailabilitySlot } from './types'

export interface FieldAvailabilitiesResponse {
  items: FieldAvailabilityItem[]
}

export const fieldAvailabilityService = {
  get: (leagueId: string, fieldId: string, signal?: AbortSignal) =>
    apiClient.get<FieldAvailabilitiesResponse>(`/api/leagues/${leagueId}/fields/${fieldId}/availability`, signal),

  put: (leagueId: string, fieldId: string, slots: FieldAvailabilitySlot[], signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/fields/${fieldId}/availability`, { slots }, signal),
}
