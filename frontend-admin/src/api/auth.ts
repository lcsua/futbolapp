import { apiClient } from './apiClient'

export interface LoginRequest {
  email: string
}

export interface LoginResponse {
  userId: string
  email: string
  role: string
  token: string
}

export const authApi = {
  login: (email: string, signal?: AbortSignal) =>
    apiClient.post<LoginResponse>('/api/auth/login', { email }, signal),
}
