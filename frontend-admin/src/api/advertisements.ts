import { apiClient } from './apiClient'

export const AdvertisementSlot = {
  LeagueTop: 1,
  LeagueMiddle: 2,
  ResultsFixture: 3,
} as const

export type AdvertisementSlot = (typeof AdvertisementSlot)[keyof typeof AdvertisementSlot]

export type Advertisement = {
  id: string
  leagueId: string
  name: string
  advertiserName: string
  desktopImageUrl: string | null
  mobileImageUrl: string | null
  targetUrl: string | null
  slot: AdvertisementSlot | string | number
  startsAt: string | null
  endsAt: string | null
  priority: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export type AdvertisementWriteBody = {
  name: string
  advertiserName: string
  targetUrl: string | null
  slot: AdvertisementSlot
  startsAt: string | null
  endsAt: string | null
  priority: number
  isActive: boolean
}

export const AD_IMAGE_MAX_BYTES = 5 * 1024 * 1024
export const AD_IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp'

const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/jpg', 'image/pjpeg', 'image/png', 'image/webp'])
const ALLOWED_IMAGE_EXTENSIONS = new Set(['.jpg', '.jpeg', '.png', '.webp'])

export function normalizeAdvertisementSlot(slot: unknown): AdvertisementSlot {
  if (slot === AdvertisementSlot.LeagueTop || slot === 'LeagueTop' || slot === '1') return AdvertisementSlot.LeagueTop
  if (slot === AdvertisementSlot.LeagueMiddle || slot === 'LeagueMiddle' || slot === '2') return AdvertisementSlot.LeagueMiddle
  if (slot === AdvertisementSlot.ResultsFixture || slot === 'ResultsFixture' || slot === '3') {
    return AdvertisementSlot.ResultsFixture
  }
  return AdvertisementSlot.LeagueTop
}

export function toAdvertisementWriteBody(
  ad: Pick<Advertisement, 'name' | 'advertiserName' | 'targetUrl' | 'slot' | 'startsAt' | 'endsAt' | 'priority' | 'isActive'>,
  overrides?: Partial<AdvertisementWriteBody>,
): AdvertisementWriteBody {
  return {
    name: ad.name,
    advertiserName: ad.advertiserName,
    targetUrl: ad.targetUrl,
    slot: normalizeAdvertisementSlot(ad.slot),
    startsAt: ad.startsAt,
    endsAt: ad.endsAt,
    priority: ad.priority,
    isActive: ad.isActive,
    ...overrides,
  }
}

export type AdvertisementImageValidationKey = 'ads.images.invalidFormat' | 'ads.images.tooLarge'

export function validateAdvertisementImage(file: File): AdvertisementImageValidationKey | null {
  if (file.size > AD_IMAGE_MAX_BYTES) return 'ads.images.tooLarge'

  const ext = file.name.includes('.') ? `.${file.name.split('.').pop()!.toLowerCase()}` : ''
  const typeOk = file.type ? ALLOWED_IMAGE_TYPES.has(file.type.toLowerCase()) : false
  const extOk = ALLOWED_IMAGE_EXTENSIONS.has(ext)

  if (!typeOk && !extOk) return 'ads.images.invalidFormat'
  if (file.type && !typeOk) return 'ads.images.invalidFormat'
  return null
}

function uploadImage(leagueId: string, advertisementId: string, kind: 'desktop' | 'mobile', file: File, signal?: AbortSignal) {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient.postForm<Advertisement>(
    `/api/leagues/${leagueId}/advertisements/${advertisementId}/${kind}-image`,
    formData,
    signal,
  )
}

export const advertisementsService = {
  getByLeagueId: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<Advertisement[]>(`/api/leagues/${leagueId}/advertisements`, signal),

  getById: (leagueId: string, advertisementId: string, signal?: AbortSignal) =>
    apiClient.get<Advertisement>(`/api/leagues/${leagueId}/advertisements/${advertisementId}`, signal),

  create: (leagueId: string, body: AdvertisementWriteBody, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/advertisements`, body, signal),

  update: (leagueId: string, advertisementId: string, body: AdvertisementWriteBody, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/advertisements/${advertisementId}`, body, signal),

  remove: (leagueId: string, advertisementId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/advertisements/${advertisementId}`, signal),

  uploadDesktopImage: (leagueId: string, advertisementId: string, file: File, signal?: AbortSignal) =>
    uploadImage(leagueId, advertisementId, 'desktop', file, signal),

  uploadMobileImage: (leagueId: string, advertisementId: string, file: File, signal?: AbortSignal) =>
    uploadImage(leagueId, advertisementId, 'mobile', file, signal),

  deleteDesktopImage: (leagueId: string, advertisementId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/advertisements/${advertisementId}/desktop-image`, signal),

  deleteMobileImage: (leagueId: string, advertisementId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/advertisements/${advertisementId}/mobile-image`, signal),
}
