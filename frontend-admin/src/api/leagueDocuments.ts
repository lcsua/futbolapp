import { apiClient } from './apiClient'

export type DocumentCategory = {
  id: string
  name: string
  slug: string
  sortOrder: number
  requiresDocumentDate: boolean
  isActive: boolean
}

export type LeagueDocument = {
  id: string
  categoryId: string
  title: string
  description?: string | null
  fileUrl: string
  relativePath: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  documentDate?: string | null
  sortOrder: number
  isPublished: boolean
}

export type UploadDocumentResult = {
  url: string
  relativeUrl: string
  contentType: string
  fileSizeBytes: number
  originalFileName: string
}

export type CreateDocumentBody = {
  categoryId: string
  title: string
  description?: string | null
  documentDate?: string | null
  fileUrl: string
  relativePath: string
  contentType: string
  fileSizeBytes: number
  originalFileName: string
  sortOrder?: number
  isPublished?: boolean
}

export type UpdateDocumentBody = {
  title: string
  description?: string | null
  documentDate?: string | null
  sortOrder?: number
  isPublished?: boolean
  fileUrl?: string
  relativePath?: string
  contentType?: string
  fileSizeBytes?: number
  originalFileName?: string
}

export const leagueDocumentsService = {
  getCategories: (leagueId: string, signal?: AbortSignal) =>
    apiClient.get<DocumentCategory[]>(`/api/leagues/${leagueId}/document-categories`, signal),

  createCategory: (
    leagueId: string,
    body: { name: string; requiresDocumentDate?: boolean; sortOrder?: number },
    signal?: AbortSignal,
  ) =>
    apiClient.post<{ id: string; slug: string }>(
      `/api/leagues/${leagueId}/document-categories`,
      body,
      signal,
    ),

  updateCategory: (
    leagueId: string,
    categoryId: string,
    body: {
      name: string
      requiresDocumentDate: boolean
      sortOrder: number
      isActive: boolean
      slug?: string
    },
    signal?: AbortSignal,
  ) => apiClient.put<void>(`/api/leagues/${leagueId}/document-categories/${categoryId}`, body, signal),

  deleteCategory: (leagueId: string, categoryId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/document-categories/${categoryId}`, signal),

  seedDefaults: (leagueId: string, signal?: AbortSignal) =>
    apiClient.post<{ createdCount: number }>(
      `/api/leagues/${leagueId}/document-categories/seed-defaults`,
      {},
      signal,
    ),

  getDocuments: (leagueId: string, categoryId?: string, signal?: AbortSignal) => {
    const q = categoryId ? `?categoryId=${encodeURIComponent(categoryId)}` : ''
    return apiClient.get<LeagueDocument[]>(`/api/leagues/${leagueId}/documents${q}`, signal)
  },

  createDocument: (leagueId: string, body: CreateDocumentBody, signal?: AbortSignal) =>
    apiClient.post<{ id: string }>(`/api/leagues/${leagueId}/documents`, body, signal),

  updateDocument: (leagueId: string, documentId: string, body: UpdateDocumentBody, signal?: AbortSignal) =>
    apiClient.put<void>(`/api/leagues/${leagueId}/documents/${documentId}`, body, signal),

  deleteDocument: (leagueId: string, documentId: string, signal?: AbortSignal) =>
    apiClient.delete(`/api/leagues/${leagueId}/documents/${documentId}`, signal),

  upload: (leagueId: string, file: File, signal?: AbortSignal) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.postForm<UploadDocumentResult>(
      `/api/leagues/${leagueId}/uploads/documents`,
      formData,
      signal,
    )
  },
}
