import type { ApiError } from './types'
import { getToken } from '../lib/authStorage'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

function buildHeaders(includeBody = false): HeadersInit {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  }
  const token = getToken()
  if (token) headers['Authorization'] = `Bearer ${token}`
  if (includeBody) headers['Content-Type'] = 'application/json'
  return headers
}

async function handleResponse<T>(response: Response): Promise<T> {
  const text = await response.text()
  if (!response.ok) {
    let message = response.statusText
    try {
      const json = JSON.parse(text) as ApiError
      if (json.error) message = json.error
    } catch {
      if (text) message = text
    }
    throw new Error(message)
  }
  if (!text) return undefined as T
  return JSON.parse(text) as T
}

export const apiClient = {
  async get<T>(path: string, signal?: AbortSignal): Promise<T> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: 'GET',
      headers: buildHeaders(),
      signal,
      credentials: 'include',
    })
    return handleResponse<T>(response)
  },

  async post<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: 'POST',
      headers: buildHeaders(true),
      body: JSON.stringify(body),
      signal,
      credentials: 'include',
    })
    return handleResponse<T>(response)
  },

  async put<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: 'PUT',
      headers: buildHeaders(true),
      body: JSON.stringify(body),
      signal,
      credentials: 'include',
    })
    return handleResponse<T>(response)
  },

  async delete(path: string, signal?: AbortSignal): Promise<void> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: 'DELETE',
      headers: buildHeaders(),
      signal,
      credentials: 'include',
    })
    if (!response.ok) await handleResponse<never>(response)
  },
}
