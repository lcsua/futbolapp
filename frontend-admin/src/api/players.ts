import { apiClient } from './apiClient'

export type PlayerPosition = 'GK' | 'DEF' | 'MID' | 'FWD'

export interface Player {
  id: string
  teamId: string
  firstName: string
  lastName: string
  nickname: string
  document: string
  position: PlayerPosition | string | null
  birthDate: string | null
  isActive: boolean
  displayName: string
}

export interface PlayerWriteBody {
  firstName: string
  lastName: string
  nickname?: string
  document?: string
  position?: PlayerPosition | '' | null
  birthDate?: string | null
  isActive?: boolean
  jerseyNumber?: number | null
}

export interface ImportPlayerItem {
  firstName: string
  lastName: string
  nickname?: string
  document?: string
  position?: PlayerPosition | string
}

export const PLAYER_POSITIONS: { value: PlayerPosition; label: string }[] = [
  { value: 'GK', label: 'Arquero' },
  { value: 'DEF', label: 'Defensor' },
  { value: 'MID', label: 'Mediocampista' },
  { value: 'FWD', label: 'Delantero' },
]

export const playersService = {
  listByTeam: (leagueId: string, teamId: string, signal?: AbortSignal) =>
    apiClient.get<Player[]>(`/api/leagues/${leagueId}/teams/${teamId}/players`, signal),

  listByTeamIds: (leagueId: string, teamIds: string[], signal?: AbortSignal) => {
    const ids = teamIds.filter(Boolean).join(',')
    return apiClient.get<Player[]>(
      `/api/leagues/${leagueId}/teams/players?teamIds=${encodeURIComponent(ids)}`,
      signal
    )
  },

  create: (leagueId: string, teamId: string, body: PlayerWriteBody, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/teams/${teamId}/players`, body, signal),

  update: (leagueId: string, teamId: string, playerId: string, body: PlayerWriteBody, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/teams/${teamId}/players/${playerId}`, body, signal),

  remove: (leagueId: string, teamId: string, playerId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/teams/${teamId}/players/${playerId}`, signal),

  import: (leagueId: string, teamId: string, players: ImportPlayerItem[], signal?: AbortSignal) =>
    apiClient.post<{ createdCount: number; playerIds: string[] }>(
      `/api/leagues/${leagueId}/teams/${teamId}/players/import`,
      { players },
      signal
    ),
}
