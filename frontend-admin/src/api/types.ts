export interface League {
  id: string
  name: string
  slug: string
  country: string
  description: string
  logoUrl: string
  isPublic: boolean
  isActive: boolean
}

export interface LeagueFormData {
  name: string
  country: string
  slug?: string
  description: string
  logoUrl: string
  isPublic: boolean
  isActive: boolean
}

export interface Season {
  id: string
  name: string
  startDate: string
  endDate: string | null
}

export interface SeasonFormData {
  name: string
  startDate: string
  endDate: string
}

export interface Division {
  id: string
  leagueId: string
  name: string
  description: string | null
  /** When true, fixture generation will not assign kickoffs in [start, end). */
  kickoffRestrictionEnabled?: boolean
  kickoffRestrictionStart?: string | null
  kickoffRestrictionEnd?: string | null
}

export interface DivisionFormData {
  name: string
  description: string
  kickoffRestrictionEnabled: boolean
  kickoffRestrictionStart: string | null
  kickoffRestrictionEnd: string | null
}

export interface Field {
  id: string
  name: string
  address: string
  city: string
  geoLat: number | null
  geoLng: number | null
  isAvailable: boolean
  description: string
}

export interface FieldFormData {
  name: string
  address: string
  city: string
  geoLat: number | null
  geoLng: number | null
  isAvailable: boolean
  description: string
}

export interface Team {
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

export interface TeamFormData {
  name: string
  suffix?: string
  clubId?: string
  shortName?: string
  primaryColor?: string
  secondaryColor?: string
  foundedYear?: number | null
  delegateName?: string
  delegateContact?: string
  email?: string
  logoUrl?: string
  photoUrl?: string
  seasonId?: string
  divisionId?: string
}

export interface Club {
  id: string
  name: string
  logoUrl: string
}

export interface ApiError {
  error: string
}

export interface CompetitionRule {
  id: string
  leagueId: string
  seasonId: string | null
  matchesPerWeek: number
  isHomeAway: boolean
  matchDays: number[]
}

export interface CompetitionRuleFormData {
  seasonId?: string | null
  matchesPerWeek: number
  isHomeAway: boolean
  matchDays: number[]
}

export interface MatchRule {
  id: string
  leagueId: string
  seasonId: string | null
  halfMinutes: number
  breakMinutes: number
  warmupBufferMinutes: number
  slotGranularityMinutes: number
  firstMatchToleranceMinutes: number
}

export interface MatchRuleFormData {
  seasonId?: string | null
  halfMinutes: number
  breakMinutes: number
  warmupBufferMinutes: number
  slotGranularityMinutes: number
  firstMatchToleranceMinutes: number
}

export interface FieldAvailabilityItem {
  id: string
  dayOfWeek: number
  startTime: string
  endTime: string
  isActive: boolean
}

export interface FieldAvailabilitySlot {
  dayOfWeek: number
  startTime: string
  endTime: string
  isActive: boolean
}

export interface FieldBlackoutItem {
  id: string
  date: string
  startTime: string
  endTime: string
  reason: string
}

export interface FieldBlackoutFormData {
  date: string
  startTime: string
  endTime: string
  reason: string
}
