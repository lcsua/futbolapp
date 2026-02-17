import { apiClient } from './apiClient'
import type { MatchRule, MatchRuleFormData } from './types'

function buildQuery(seasonId?: string | null): string {
  if (seasonId) return `?seasonId=${encodeURIComponent(seasonId)}`
  return ''
}

export const matchRulesService = {
  get: (leagueId: string, seasonId?: string | null, signal?: AbortSignal) =>
    apiClient.get<MatchRule>(`/api/leagues/${leagueId}/match-rules${buildQuery(seasonId)}`, signal),

  put: (leagueId: string, data: MatchRuleFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/match-rules`, { ...data, seasonId: data.seasonId ?? null }, signal),
}
