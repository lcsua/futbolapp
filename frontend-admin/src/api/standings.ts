import { apiClient } from './apiClient'

export interface TeamStanding {
  position: number
  teamId: string
  teamName: string
  points: number
  played: number
  wins: number
  draws: number
  losses: number
  goalsFor: number
  goalsAgainst: number
  goalDifference: number
}

export interface DivisionStandings {
  divisionId: string
  divisionName: string
  standings: TeamStanding[]
}

export const standingsService = {
  get: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.get<DivisionStandings[]>(`/api/leagues/${leagueId}/seasons/${seasonId}/standings`, signal),
}
