import { apiClient } from './apiClient'

export interface MatchListItem {
  id: string
  divisionSeasonId: string
  divisionName: string
  roundNumber: number
  homeTeamName: string
  homeTeamId: string
  awayTeamName: string
  awayTeamId: string
  homeScore: number | null
  awayScore: number | null
  status: string
  kickoffTime: string
  matchDate: string
  fieldName: string
  homeTeamLogoUrl?: string | null
  awayTeamLogoUrl?: string | null
}

export interface MatchRoundGroup {
  roundNumber: number
  divisionName: string
  matches: MatchListItem[]
}

export interface GetMatchesResponse {
  rounds: MatchRoundGroup[]
}

export interface MatchIncidentDto {
  id: string
  minute: number
  teamId: string | null
  teamName: string | null
  playerName: string
  incidentType: string
  notes: string
}

export interface MatchDetailResponse {
  id: string
  roundNumber: number
  divisionName: string
  homeTeamName: string
  homeTeamId: string
  awayTeamName: string
  awayTeamId: string
  homeScore: number | null
  awayScore: number | null
  status: string
  kickoffTime: string
  matchDate: string
  fieldName: string
  incidents: MatchIncidentDto[]
  homeTeamLogoUrl?: string | null
  awayTeamLogoUrl?: string | null
}

export interface UpdateMatchResultBody {
  homeScore: number
  awayScore: number
  status: string
}

export interface AddIncidentBody {
  minute: number
  teamId: string | null
  playerName: string
  incidentType: string
  notes: string
}

export const matchesService = {
  getMatches: (
    leagueId: string,
    params: { seasonId: string; divisionId?: string; round?: number },
    signal?: AbortSignal
  ) => {
    const search = new URLSearchParams()
    search.set('seasonId', params.seasonId)
    if (params.divisionId) search.set('divisionId', params.divisionId)
    if (params.round != null) search.set('round', String(params.round))
    return apiClient.get<GetMatchesResponse>(
      `/api/leagues/${leagueId}/matches?${search}`,
      signal
    )
  },

  getById: (leagueId: string, matchId: string, signal?: AbortSignal) =>
    apiClient.get<MatchDetailResponse>(`/api/leagues/${leagueId}/matches/${matchId}`, signal),

  updateResult: (
    leagueId: string,
    matchId: string,
    body: UpdateMatchResultBody,
    signal?: AbortSignal
  ) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/matches/${matchId}/result`, body, signal),

  addIncident: (
    leagueId: string,
    matchId: string,
    body: AddIncidentBody,
    signal?: AbortSignal
  ) =>
    apiClient.post<{ id: string }>(
      `/api/leagues/${leagueId}/matches/${matchId}/incidents`,
      body,
      signal
    ),

  deleteIncident: (leagueId: string, incidentId: string, signal?: AbortSignal) =>
    apiClient.delete(
      `/api/leagues/${leagueId}/matches/incidents/${incidentId}`,
      signal
    ),
}

export const INCIDENT_TYPES = [
  'Goal',
  'YellowCard',
  'RedCard',
  'Injury',
  'Substitution',
  'Other',
] as const

export const MATCH_STATUSES = [
  'SCHEDULED',
  'IN_PROGRESS',
  'COMPLETED',
  'CANCELLED',
  'POSTPONED',
  'PLAYED',
] as const
