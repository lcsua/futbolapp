import { apiClient } from './apiClient'

export interface LeagueAccess {
  leagueId: string
  roleId: string | null
  roleName: string
  roleCode: string | null
  isSystemRole: boolean
  permissions: string[]
  canCreateLeague: boolean
}

export interface LeagueUser {
  userId: string
  fullName: string
  email: string
  isActive: boolean
  roleId: string | null
  roleName: string
  roleCode: string | null
  isSystemRole: boolean
}

export interface CreateLeagueUserPayload {
  fullName: string
  email: string
  password: string
  roleId: string
}

export const usersService = {
  getMyAccess: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<LeagueAccess>(`/api/leagues/${leagueId}/my-access`, signal),

  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<LeagueUser[]>(`/api/leagues/${leagueId}/users`, signal),

  create: (leagueId: string, data: CreateLeagueUserPayload, signal?: AbortSignal) =>
    apiClient.post(`/api/leagues/${leagueId}/users`, data, signal),

  updateRole: (leagueId: string, userId: string, roleId: string, signal?: AbortSignal) =>
    apiClient.put(`/api/leagues/${leagueId}/users/${userId}`, { roleId }, signal),

  remove: (leagueId: string, userId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/users/${userId}`, signal),
}
