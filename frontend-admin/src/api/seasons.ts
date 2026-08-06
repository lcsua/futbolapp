import { apiClient } from './apiClient'
import type { Season, SeasonFormData } from './types'

export interface SeasonSetupDivision {
  divisionId: string
  divisionName: string
  teams: TeamInSetup[]
}

export interface TeamInSetup {
  id: string
  name: string
  suffix?: string | null
  displayName?: string
  clubId?: string | null
  clubName?: string | null
  shortName: string | null
  logoUrl: string | null
  email: string | null
  foundedYear: number | null
  delegateName: string
  delegateContact: string
  photoUrl: string | null
}

export interface SeasonSetupResponse {
  unassignedTeams: TeamInSetup[]
  divisions: SeasonSetupDivision[]
}

export interface SaveSeasonSetupBody {
  divisions: { divisionId: string; teamIds: string[] }[]
}

export const seasonsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Season[]>(`/api/leagues/${leagueId}/seasons`, signal),

  create: (leagueId: string, data: SeasonFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(
      `/api/leagues/${leagueId}/seasons`,
      {
        name: data.name,
        startDate: data.startDate,
        endDate: data.endDate?.trim() ? data.endDate : null,
        isPublic: !!data.isPublic,
      },
      signal
    ),

  update: (leagueId: string, seasonId: string, data: SeasonFormData, signal?: AbortSignal) =>
    apiClient.put<void>(
      `/api/leagues/${leagueId}/seasons/${seasonId}`,
      {
        name: data.name,
        startDate: data.startDate,
        endDate: data.endDate?.trim() ? data.endDate : null,
        isPublic: !!data.isPublic,
      },
      signal
    ),

  close: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.post<{ pendingResultsCount: number }>(
      `/api/leagues/${leagueId}/seasons/${seasonId}/close`,
      {},
      signal
    ),

  reopen: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.post<void>(`/api/leagues/${leagueId}/seasons/${seasonId}/reopen`, {}, signal),

  delete: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/seasons/${seasonId}`, signal),

  getAssignedTeamIds: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.get<{ teamIds: string[] }>(`/api/leagues/${leagueId}/seasons/${seasonId}/assigned-team-ids`, signal),

  getSetup: (leagueId: string, seasonId: string, signal?: AbortSignal) =>
    apiClient.get<SeasonSetupResponse>(`/api/leagues/${leagueId}/seasons/${seasonId}/setup`, signal),

  saveSetup: (leagueId: string, seasonId: string, body: SaveSeasonSetupBody, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/seasons/${seasonId}/setup`, body, signal),

  copyFrom: (leagueId: string, seasonId: string, sourceSeasonId: string, signal?: AbortSignal) =>
    apiClient.post<void>(`/api/leagues/${leagueId}/seasons/${seasonId}/copy-from/${sourceSeasonId}`, {}, signal),
}
