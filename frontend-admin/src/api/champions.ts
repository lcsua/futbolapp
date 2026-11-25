import { apiClient } from './apiClient'
import { validateAdvertisementImage } from './advertisements'

export type ChampionMode = 'existing' | 'historical'

export type ChampionPhoto = {
  id: string
  imageUrl: string
  thumbUrl: string | null
  caption: string | null
  displayOrder: number
  isCover: boolean
}

export type LeagueChampion = {
  id: string
  teamId: string
  teamName: string
  teamLogoUrl: string | null
  teamIsArchived: boolean
  year: number
  championshipName: string
  divisionName: string
  seasonId: string | null
  divisionId: string | null
  wonAt: string | null
  photoCount: number
  coverImageUrl: string | null
  coverThumbUrl: string | null
  notes?: string | null
  photos?: ChampionPhoto[]
}

export type ChampionSeasonOption = {
  id: string
  name: string
  year: number
  isActive: boolean
}

export type ChampionDivisionOption = {
  id: string
  name: string
}

export type ChampionTeamOption = {
  id: string
  name: string
  logoUrl: string | null
  isArchived: boolean
  isInactive: boolean
}

export type ChampionRoster = {
  seasonId: string
  divisionId: string
  teamIds: string[]
}

export type ChampionOptions = {
  seasons: ChampionSeasonOption[]
  divisions: ChampionDivisionOption[]
  teams: ChampionTeamOption[]
  participants: ChampionRoster[]
}

export type ChampionWriteBody = {
  mode: ChampionMode
  teamId: string
  seasonId: string | null
  divisionId: string | null
  year: number | null
  championshipName: string | null
  divisionName: string | null
  wonAt: string | null
  notes: string | null
}

export type ChampionListFilters = {
  year?: number | null
  championship?: string
  division?: string
  teamId?: string
}

export type ChampionGalleryPhoto = {
  id: string
  caption: string | null
  displayOrder: number
  isCover: boolean
}

function listQuery(filters?: ChampionListFilters) {
  const params = new URLSearchParams()
  if (filters?.year) params.set('year', String(filters.year))
  if (filters?.championship?.trim()) params.set('championship', filters.championship.trim())
  if (filters?.division?.trim()) params.set('division', filters.division.trim())
  if (filters?.teamId) params.set('teamId', filters.teamId)
  const query = params.toString()
  return query ? `?${query}` : ''
}

export function validateChampionImage(file: File) {
  return validateAdvertisementImage(file)
}

export const championsService = {
  list: (leagueId: string, filters?: ChampionListFilters, signal?: AbortSignal) =>
    apiClient.get<LeagueChampion[]>(`/api/leagues/${leagueId}/champions${listQuery(filters)}`, signal),

  options: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<ChampionOptions>(`/api/leagues/${leagueId}/champions/options`, signal),

  get: (leagueId: string, championId: string, signal?: AbortSignal) =>
    apiClient.get<LeagueChampion>(`/api/leagues/${leagueId}/champions/${championId}`, signal),

  create: (leagueId: string, body: ChampionWriteBody, signal?: AbortSignal) =>
    apiClient.post<LeagueChampion>(`/api/leagues/${leagueId}/champions`, body, signal),

  update: (leagueId: string, championId: string, body: ChampionWriteBody, signal?: AbortSignal) =>
    apiClient.put<LeagueChampion>(`/api/leagues/${leagueId}/champions/${championId}`, body, signal),

  remove: (leagueId: string, championId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/champions/${championId}`, signal),

  uploadPhotos: (leagueId: string, championId: string, files: File[], signal?: AbortSignal) => {
    const formData = new FormData()
    files.forEach((file) => formData.append('files', file))
    return apiClient.postForm<LeagueChampion>(
      `/api/leagues/${leagueId}/champions/${championId}/photos`,
      formData,
      signal,
    )
  },

  updateGallery: (leagueId: string, championId: string, photos: ChampionGalleryPhoto[], signal?: AbortSignal) =>
    apiClient.put<LeagueChampion>(`/api/leagues/${leagueId}/champions/${championId}/photos`, { photos }, signal),

  deletePhoto: (leagueId: string, championId: string, photoId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/champions/${championId}/photos/${photoId}`, signal),
}
