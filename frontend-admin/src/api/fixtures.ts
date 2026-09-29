import { apiClient } from './apiClient'

export interface FixtureDraftMatch {
  divisionSeasonId: string
  divisionName: string
  homeTeamDivisionSeasonId: string
  homeTeamName: string
  awayTeamDivisionSeasonId: string
  awayTeamName: string
  fieldId?: string | null
  fieldName?: string | null
  date?: string | null
  kickoffTime?: string | null
  isInterzonal?: boolean
  awayDivisionName?: string | null
  isReplanned?: boolean
}

export interface FixtureDraftByeTeam {
  divisionSeasonId: string
  divisionName: string
  teamDivisionSeasonId: string
  teamName: string
}

export interface FixtureDraftRound {
  roundNumber: number
  matchDate?: string | null
  matches: FixtureDraftMatch[]
  byeTeams?: FixtureDraftByeTeam[] | null
}

export interface FixtureReplan {
  fromRound: number
  divisionSeasonIds: string[]
  removedFixtureIds: string[]
  warnings: string[]
}

export interface FixtureDraft {
  rounds: FixtureDraftRound[]
  replan?: FixtureReplan | null
}

export interface ReplanFixturesBody {
  divisionIds: string[]
  fromRound: number
  fillByesWithInterzonal: boolean
}

export interface RoundInterzonalTeam {
  teamDivisionSeasonId: string
  teamName: string
  /** Already plays a non-interzonal match that round. */
  isBusy: boolean
}

export interface RoundInterzonalZone {
  divisionSeasonId: string
  divisionId: string
  divisionName: string
  teams: RoundInterzonalTeam[]
}

export interface RoundInterzonalPair {
  fixtureId?: string | null
  homeTeamDivisionSeasonId: string
  homeTeamName: string
  awayTeamDivisionSeasonId: string
  awayTeamName: string
  isLocked: boolean
  fieldName?: string | null
  startTime?: string | null
}

export interface RoundInterzonalMatches {
  roundNumber: number
  roundDate?: string | null
  zones: RoundInterzonalZone[]
  pairs: RoundInterzonalPair[]
  warnings: string[]
}

export interface SetRoundInterzonalPair {
  homeTeamDivisionSeasonId: string
  awayTeamDivisionSeasonId: string
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

export interface CopyFixturesFromSeasonBody {
  sourceSeasonId: string
  divisionId?: string | null
  invertHomes: boolean
}

export interface CopyFixturesFromSeasonResponse {
  copiedCount: number
  errors: string[]
}

export interface AssignFixtureDatesBody {
  firstRoundDate: string // yyyy-MM-dd
  divisionId?: string | null
}

export interface AssignFixtureDatesResponse {
  updatedCount: number
  roundCount: number
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

  replan: (leagueId: string, seasonId: string, body: ReplanFixturesBody, signal?: AbortSignal) =>
    apiClient.post<FixtureDraft>(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/replan`, body, signal),

  discardDraft: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/draft`, signal),

  getRoundInterzonal: (
    leagueId: string,
    seasonId: string,
    round: number,
    divisionIds: string[],
    signal?: AbortSignal,
  ) => {
    const query = new URLSearchParams()
    for (const id of divisionIds) query.append('divisionIds', id)
    return apiClient.get<RoundInterzonalMatches>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/rounds/${round}/interzonal?${query.toString()}`,
      signal,
    )
  },

  setRoundInterzonal: (
    leagueId: string,
    seasonId: string,
    round: number,
    body: { divisionIds: string[]; pairs: SetRoundInterzonalPair[] },
    signal?: AbortSignal,
  ) =>
    apiClient.put<RoundInterzonalMatches>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/rounds/${round}/interzonal`,
      body,
      signal,
    ),

  previewImport: (leagueId: string, body: FixtureImportBody, signal?: AbortSignal) =>
    apiClient.post<PreviewFixtureImportResponse>(`/api/leagues/${leagueId}/fixtures/import/preview`, body, signal),

  importFixtures: (leagueId: string, body: FixtureImportBody, signal?: AbortSignal) =>
    apiClient.post<ImportFixturesResponse>(`/api/leagues/${leagueId}/fixtures/import`, body, signal),

  copyFromSeason: (
    leagueId: string,
    targetSeasonId: string,
    body: CopyFixturesFromSeasonBody,
    signal?: AbortSignal
  ) =>
    apiClient.post<CopyFixturesFromSeasonResponse>(
      `/api/leagues/${leagueId}/seasons/${targetSeasonId}/fixtures/copy`,
      body,
      signal
    ),

  assignDates: (
    leagueId: string,
    seasonId: string,
    body: AssignFixtureDatesBody,
    signal?: AbortSignal
  ) =>
    apiClient.post<AssignFixtureDatesResponse>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/fixtures/assign-dates`,
      body,
      signal
    ),
}
