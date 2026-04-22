import { apiClient } from './apiClient'

export interface MatchRuleSchedulingSummary {
  id: string
  leagueId: string
  seasonId: string | null
  halfMinutes: number
  breakMinutes: number
  warmupBufferMinutes: number
  derivedTotalMatchSlotMinutes: number
  slotGranularityMinutes: number
  firstMatchToleranceMinutes: number
}

export interface DivisionMatchRulesExtras {
  divisionSeasonId: string
  halfMinutes: number | null
  breakMinutes: number | null
  warmupBufferMinutes: number | null
  slotGranularityMinutes: number | null
  firstMatchToleranceMinutes: number | null
  breakBetweenMatchesMinutes: number | null
  allowedTimeRangesJson: string | null
}

export interface EffectiveKickoffTimeRange {
  start: string
  end: string
}

export interface EffectiveMatchRules {
  totalMatchSlotBlockMinutes: number
  slotGranularityMinutes: number
  firstMatchToleranceMinutes: number
  breakBetweenMatchesMinutes: number
  allowedFieldIds: string[] | null
  allowedKickoffTimeRanges: EffectiveKickoffTimeRange[] | null
}

export interface DivisionSchedulingExtrasBundle {
  divisionSeasonId: string
  globalMatchRule: MatchRuleSchedulingSummary
  divisionExtras: DivisionMatchRulesExtras | null
  explicitFieldIds: string[]
  effectivePreview: EffectiveMatchRules
}

export interface UpsertDivisionSchedulingExtrasBody {
  halfMinutes?: number | null
  breakMinutes?: number | null
  warmupBufferMinutes?: number | null
  slotGranularityMinutes?: number | null
  firstMatchToleranceMinutes?: number | null
  breakBetweenMatchesMinutes?: number | null
  allowedTimeRangesJson?: string | null
  explicitFieldIds?: string[] | null
}

export const schedulingExtrasService = {
  getDivisionBundle: (leagueId: string, seasonId: string, divisionId: string, signal?: AbortSignal) =>
    apiClient.get<DivisionSchedulingExtrasBundle>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/divisions/${divisionId}/scheduling-extras`,
      signal,
    ),

  putDivisionExtras: (
    leagueId: string,
    seasonId: string,
    divisionId: string,
    body: UpsertDivisionSchedulingExtrasBody,
    signal?: AbortSignal,
  ) =>
    apiClient.put<void>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/divisions/${divisionId}/scheduling-extras`,
      body,
      signal,
    ),
}
