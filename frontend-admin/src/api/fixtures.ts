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

export interface FixtureDraftByeTeam {
  divisionSeasonId: string
  divisionName: string
  teamDivisionSeasonId: string
  teamName: string
}

export interface FixtureDraftRound {
  roundNumber: number
  matchDate: string
  matches: FixtureDraftMatch[]
  byeTeams?: FixtureDraftByeTeam[] | null
}

export interface FixtureDraft {
  rounds: FixtureDraftRound[]
}

export interface GetFixturesResponse {
  fixtures: FixtureDraft
  isDraft: boolean
}

export interface PreviewFixtureRow {
  round: number
  date?: string | null
  time?: string | null
  field?: string | null
  homeTeam: string
  awayTeam: string
  rowError?: string | null
}

export interface PreviewFixtureImportResponse {
  importType: string
  rows: PreviewFixtureRow[]
  errors: string[]
}

export interface ImportFixturesResponse {
  importedCount: number
  errors: string[]
}

export interface FixtureImportBody {
  seasonId: string
  divisionId: string
  csvText: string
}

export const fixturesService = {
  get: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.get<GetFixturesResponse>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures`, signal),

  generate: (leagueId: string, seasonId: string, divisionId?: string, signal?: AbortSignal) =>
    apiClient.post<FixtureDraft>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/generate`,
      { divisionId: divisionId || undefined },
      signal,
    ),

  commit: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.post<void>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/commit`, {}, signal),

  previewImport: (leagueId: string, body: FixtureImportBody, signal?: AbortSignal) =>
    apiClient.post<PreviewFixtureImportResponse>(`/api/leagues/${leagueId}/fixtures/import/preview`, body, signal),

  importFixtures: (leagueId: string, body: FixtureImportBody, signal?: AbortSignal) =>
    apiClient.post<ImportFixturesResponse>(`/api/leagues/${leagueId}/fixtures/import`, body, signal),
}
