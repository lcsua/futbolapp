import { apiClient } from './apiClient'

export interface AuthCapabilities {
  userId: string
  email: string
  role: string
  canCreateLeague: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  userId: string
  email: string
  role: string
  token: string
}

export const authApi = {
  login: (email: string, password: string, signal?: AbortSignal) =>
    apiClient.post<LoginResponse>('/api/auth/login', { email, password }, signal),

  me: (signal?: AbortSignal) => apiClient.get<AuthCapabilities>('/api/auth/me', signal),
}
