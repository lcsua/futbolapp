import { apiClient } from './apiClient'
import type { CompetitionRule, CompetitionRuleFormData } from './types'

function buildQuery(seasonId?: string | null): string {
  if (seasonId) return `?seasonId=${encodeURIComponent(seasonId)}`
  return ''
}

export const competitionRulesService = {
  get: (leagueId: string, seasonId?: string | null, signal?: AbortSignal) =>
    apiClient.get<CompetitionRule>(`/api/leagues/${leagueId}/competition-rules${buildQuery(seasonId)}`, signal),

  put: (leagueId: string, data: CompetitionRuleFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/competition-rules`, { ...data, seasonId: data.seasonId ?? null }, signal),
}
