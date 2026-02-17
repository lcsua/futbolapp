export interface League {
  id: string
  name: string
  country: string
  description: string
  logoUrl: string
}

export interface LeagueFormData {
  name: string
  country: string
  description: string
  logoUrl: string
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
}

export interface DivisionFormData {
  name: string
  description: string
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
  shortName?: string
  primaryColor?: string
  secondaryColor?: string
  foundedYear?: number | null
  delegateName?: string
  delegateContact?: string
  email?: string
  logoUrl?: string
  photoUrl?: string
}

export interface ApiError {
  error: string
}
