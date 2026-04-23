import { apiClient } from './apiClient'

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
}
