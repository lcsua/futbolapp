import { AdvertisementSlot, normalizeAdvertisementSlot } from '../api/advertisements'

export type AdvertisementVisualStatus = 'inactive' | 'scheduled' | 'expired' | 'active'

export function getAdvertisementVisualStatus(
  ad: { isActive: boolean; startsAt?: string | null; endsAt?: string | null },
  now = new Date(),
): AdvertisementVisualStatus {
  if (!ad.isActive) return 'inactive'

  if (ad.startsAt) {
    const startsAt = new Date(ad.startsAt)
    if (!Number.isNaN(startsAt.getTime()) && startsAt.getTime() > now.getTime()) return 'scheduled'
  }

  if (ad.endsAt) {
    const endsAt = new Date(ad.endsAt)
    if (!Number.isNaN(endsAt.getTime()) && endsAt.getTime() < now.getTime()) return 'expired'
  }

  return 'active'
}

export const AD_SLOT_OPTIONS: { value: AdvertisementSlot; labelKey: string }[] = [
  { value: AdvertisementSlot.LeagueTop, labelKey: 'ads.slots.leagueTop' },
  { value: AdvertisementSlot.LeagueMiddle, labelKey: 'ads.slots.leagueMiddle' },
  { value: AdvertisementSlot.ResultsFixture, labelKey: 'ads.slots.resultsFixture' },
]

export function advertisementSlotLabelKey(slot: unknown): string {
  const normalized = normalizeAdvertisementSlot(slot)
  return AD_SLOT_OPTIONS.find((option) => option.value === normalized)?.labelKey ?? 'ads.slots.leagueTop'
}

export function toDatetimeLocalValue(iso: string | null | undefined): string {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function fromDatetimeLocalValue(value: string): string | null {
  const trimmed = value.trim()
  if (!trimmed) return null
  const date = new Date(trimmed)
  if (Number.isNaN(date.getTime())) return null
  return date.toISOString()
}

export function formatAdvertisementPeriod(
  startsAt: string | null | undefined,
  endsAt: string | null | undefined,
  labels: { none: string; from: string; until: string },
  locale: string,
): string {
  const format = (iso: string) => {
    const date = new Date(iso)
    if (Number.isNaN(date.getTime())) return iso
    return date.toLocaleString(locale, { dateStyle: 'short', timeStyle: 'short' })
  }

  if (!startsAt && !endsAt) return labels.none
  if (startsAt && endsAt) return `${format(startsAt)} – ${format(endsAt)}`
  if (startsAt) return `${labels.from} ${format(startsAt)}`
  return `${labels.until} ${format(endsAt!)}`
}
