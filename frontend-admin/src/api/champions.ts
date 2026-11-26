import { apiClient } from './apiClient'

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

const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/jpg', 'image/pjpeg', 'image/png', 'image/webp'])
const ALLOWED_IMAGE_EXTENSIONS = new Set(['.jpg', '.jpeg', '.png', '.webp'])
const CHAMPION_PHOTO_MAX_EDGE = 1920
const CHAMPION_PHOTO_TARGET_BYTES = 700 * 1024

export function validateChampionImage(file: File): 'ads.images.invalidFormat' | null {
  const ext = file.name.includes('.') ? `.${file.name.split('.').pop()!.toLowerCase()}` : ''
  const typeOk = file.type ? ALLOWED_IMAGE_TYPES.has(file.type.toLowerCase()) : false
  const extOk = ALLOWED_IMAGE_EXTENSIONS.has(ext)
  if (!typeOk && !extOk) return 'ads.images.invalidFormat'
  if (file.type && !typeOk) return 'ads.images.invalidFormat'
  return null
}

function canvasToJpeg(canvas: HTMLCanvasElement, quality: number): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (blob) => (blob ? resolve(blob) : reject(new Error('No se pudo preparar la fotografía.'))),
      'image/jpeg',
      quality,
    )
  })
}

function drawFitted(source: CanvasImageSource, sourceWidth: number, sourceHeight: number, maxEdge: number) {
  const scale = Math.min(1, maxEdge / Math.max(sourceWidth, sourceHeight))
  const width = Math.max(1, Math.round(sourceWidth * scale))
  const height = Math.max(1, Math.round(sourceHeight * scale))
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  const context = canvas.getContext('2d')
  if (!context) throw new Error('No se pudo preparar la fotografía.')
  context.fillStyle = '#ffffff'
  context.fillRect(0, 0, width, height)
  context.drawImage(source, 0, 0, width, height)
  return canvas
}

export async function shrinkChampionPhoto(file: File): Promise<File> {
  const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  try {
    let maxEdge = CHAMPION_PHOTO_MAX_EDGE
    let quality = 0.82
    let blob = await canvasToJpeg(drawFitted(bitmap, bitmap.width, bitmap.height, maxEdge), quality)
    while (blob.size > CHAMPION_PHOTO_TARGET_BYTES && (quality > 0.5 || maxEdge > 640)) {
      if (quality > 0.5) quality = Math.round((quality - 0.08) * 100) / 100
      else {
        maxEdge = Math.round(maxEdge * 0.75)
        quality = 0.72
      }
      blob = await canvasToJpeg(drawFitted(bitmap, bitmap.width, bitmap.height, maxEdge), quality)
    }

    const base = file.name.replace(/\.[^.]+$/, '').trim() || 'foto'
    return new File([blob], `${base}.jpg`, { type: 'image/jpeg', lastModified: Date.now() })
  } finally {
    bitmap.close()
  }
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

  uploadPhotos: async (leagueId: string, championId: string, files: File[], signal?: AbortSignal) => {
    const prepared: File[] = []
    for (const file of files) {
      try {
        prepared.push(await shrinkChampionPhoto(file))
      } catch {
        throw new Error('No se pudo leer una de las fotografías. Probá con otra imagen.')
      }
    }

    let latest: LeagueChampion | null = null
    for (const file of prepared) {
      const formData = new FormData()
      formData.append('files', file)
      latest = await apiClient.postForm<LeagueChampion>(
        `/api/leagues/${leagueId}/champions/${championId}/photos`,
        formData,
        signal,
      )
    }
    if (!latest) throw new Error('Seleccioná al menos una imagen.')
    return latest
  },

  updateGallery: (leagueId: string, championId: string, photos: ChampionGalleryPhoto[], signal?: AbortSignal) =>
    apiClient.put<LeagueChampion>(`/api/leagues/${leagueId}/champions/${championId}/photos`, { photos }, signal),

  deletePhoto: (leagueId: string, championId: string, photoId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/champions/${championId}/photos/${photoId}`, signal),
}
