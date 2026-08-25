import { apiClient } from './apiClient'

export interface RoleItem {
  id: string
  name: string
  description: string
  code: string | null
  isSystem: boolean
  permissions: string[]
}

export interface PermissionCatalogItem {
  code: string
  name: string
  module: string
}

export interface SaveRolePayload {
  name: string
  description: string
  permissionCodes: string[]
}

export const rolesService = {
  getCatalog: (signal?: AbortSignal) =>
    apiClient.get<PermissionCatalogItem[]>('/api/permissions', signal),

  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<RoleItem[]>(`/api/leagues/${leagueId}/roles`, signal),

  create: (leagueId: string, data: SaveRolePayload, signal?: AbortSignal) =>
    apiClient.post<RoleItem>(`/api/leagues/${leagueId}/roles`, data, signal),

  update: (leagueId: string, roleId: string, data: SaveRolePayload, signal?: AbortSignal) =>
    apiClient.put<RoleItem>(`/api/leagues/${leagueId}/roles/${roleId}`, data, signal),

  remove: (leagueId: string, roleId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/roles/${roleId}`, signal),
}
