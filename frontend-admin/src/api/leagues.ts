import { apiClient } from './apiClient'
import type { League, LeagueFormData } from './types'

function generateSlug(input: string): string {
  const base = input
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')

  const cleaned = base
    .split('-')
    .filter((token) => token && token !== 'liga' && token !== 'ligas')
    .join('-')

  return cleaned || base
}

export const leaguesService = {
  generateSlug,

  getMyLeagues: (signal?: AbortSignal) =>
    apiClient.get<League[]>('/api/leagues', signal),

  getById: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<League>(`/api/leagues/${leagueId}`, signal),

  create: (data: LeagueFormData, signal?: AbortSignal) =>
    apiClient.post<{ id: string; slug: string }>('/api/leagues', data, signal),

  update: (leagueId: string, data: LeagueFormData, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}`, data, signal),

  uploadImage: async (leagueId: string, file: File, signal?: AbortSignal) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.postForm<{ url: string; relativeUrl: string }>(
      `/api/leagues/${leagueId}/uploads/images`,
      formData,
      signal
    )
  },

  checkSlugAvailability: (slug: string, excludeLeagueId?: string, signal?: AbortSignal) => {
    const params = new URLSearchParams({ slug })
    if (excludeLeagueId) params.set('excludeLeagueId', excludeLeagueId)
    return apiClient.get<{ available: boolean }>(`/api/leagues/check-slug?${params}`, signal)
  },
}
