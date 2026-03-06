import { apiClient } from './apiClient'

export interface FixtureDraftMatch {
  divisionSeasonId: string
  divisionName: string
  homeTeamDivisionSeasonId: string
  homeTeamName: string
  awayTeamDivisionSeasonId: string
  awayTeamName: string
  fieldId: string
  fieldName: string
  date: string
  kickoffTime: string
}

export interface FixtureDraftRound {
  roundNumber: number
  matchDate: string
  matches: FixtureDraftMatch[]
}

export interface FixtureDraft {
  rounds: FixtureDraftRound[]
}

export interface GetFixturesResponse {
  fixtures: FixtureDraft
  isDraft: boolean
}

export const fixturesService = {
  get: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.get<GetFixturesResponse>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures`, signal),

  generate: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.post<FixtureDraft>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/generate`, {}, signal),

  commit: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.post<void>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/commit`, {}, signal),
}
