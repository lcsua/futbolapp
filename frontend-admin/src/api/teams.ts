import { apiClient } from './apiClient'
import type { Team, TeamFormData } from './types'

export const teamsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Team[]>(`/api/leagues/${leagueId}/teams`, signal),

  create: (leagueId: string, data: { name: string; shortName?: string; email?: string }, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/teams`, data, signal),

  update: (leagueId: string, teamId: string, data: TeamFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/teams/${teamId}`, data, signal),

  assignDivisionToSeason: (leagueId: string, seasonId: string, divisionId: string, signal?: AbortSignal) =>
    apiClient.post<{ divisionSeasonId: string }>(`/api/leagues/${leagueId}/seasons/${seasonId}/divisions`, { divisionId }, signal),

  assignTeamToDivisionSeason: (leagueId: string, seasonId: string, divisionId: string, teamId: string, signal?: AbortSignal) =>
    apiClient.post<{ teamDivisionSeasonId: string }>(`/api/leagues/${leagueId}/seasons/${seasonId}/divisions/${divisionId}/teams`, { teamId }, signal),

  bulkCreate: (leagueId: string, names: string[], signal?: AbortSignal) =>
    apiClient.post<{ createdIds: string[] }>(`/api/leagues/${leagueId}/teams/bulk`, { names }, signal),
}
