import { apiClient } from './apiClient'
import type { Club, Team, TeamFormData } from './types'

export const teamsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Team[]>(`/api/leagues/${leagueId}/teams`, signal),

  create: (
    leagueId: string,
    data: { name: string; shortName?: string; email?: string; suffix?: string; clubId?: string; seasonId?: string; divisionId?: string },
    signal?: AbortSignal
  ) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/teams`, data, signal),

  createClub: (leagueId: string, data: { name: string; logoUrl?: string }, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/clubs`, data, signal),

  updateClub: (leagueId: string, clubId: string, data: { name: string; logoUrl?: string }, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/clubs/${clubId}`, data, signal),

  uploadImage: async (leagueId: string, file: File, signal?: AbortSignal) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.postForm<{ url: string; relativeUrl: string }>(`/api/leagues/${leagueId}/uploads/images`, formData, signal)
  },

  getClubsByLeague: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Club[]>(`/api/leagues/${leagueId}/clubs`, signal),

  update: (leagueId: string, teamId: string, data: TeamFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/teams/${teamId}`, data, signal),

  assignDivisionToSeason: (leagueId: string, seasonId: string, divisionId: string, signal?: AbortSignal) =>
    apiClient.post<{ divisionSeasonId: string }>(`/api/leagues/${leagueId}/seasons/${seasonId}/divisions`, { divisionId }, signal),

  assignTeamToDivisionSeason: (leagueId: string, seasonId: string, divisionId: string, teamId: string, signal?: AbortSignal) =>
    apiClient.post<{ teamDivisionSeasonId: string }>(`/api/leagues/${leagueId}/seasons/${seasonId}/divisions/${divisionId}/teams`, { teamId }, signal),

  bulkCreate: (leagueId: string, names: string[], signal?: AbortSignal) =>
    apiClient.post<{ createdIds: string[] }>(`/api/leagues/${leagueId}/teams/bulk`, { names }, signal),
}
