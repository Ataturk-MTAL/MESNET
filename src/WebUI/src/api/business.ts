import api from 'boot/axios'
import type { GeoLocation } from './institution'

export interface BusinessCapacityDto {
  totalSlots: number
  occupiedSlots: number
  availableSlots: number
  isFull: boolean
}

export interface BusinessRepresentativeDto {
  id: string
  fullName: string
  phoneNumber: string
  email: string
  authorizedAt: string
}

export interface BusinessDocumentDto {
  id: string
  type: string
  typeSlug: string
  status: string
  statusSlug: string
  fileName: string
  uploadedAt: string
  approvedAt: string | null
  expiresAt: string | null
  rejectionReason: string | null
}

export interface SectorDto {
  name: string
  slug: string
}

export interface BusinessDto {
  id: string
  name: string
  address: string
  phoneNumber: string | null
  email: string | null
  website: string | null
  status: string
  statusSlug: string
  source: string
  sourceSlug: string
  personnelCount: number
  location: GeoLocation | null
  capacity: BusinessCapacityDto
  representatives: BusinessRepresentativeDto[]
  documents: BusinessDocumentDto[]
  sectors: SectorDto[]
  createdAt: string
  approvedAt: string | null
  closedAt: string | null
}

export interface RegisterBusinessRequest {
  name: string
  address: string
  phoneNumber?: string
  email?: string
  website?: string
  personnelCount?: number
  location?: GeoLocation
  sectors?: string[]
}

export interface UpdateBusinessRequest {
  name?: string
  address?: string
  phoneNumber?: string
  email?: string
  website?: string
  personnelCount?: number
  location?: GeoLocation
  sectors?: string[]
}

export interface UpdateCapacityRequest {
  totalSlots: number
}

export const businessApi = {
  list: (status?: string, sector?: string) =>
    api.get<BusinessDto[]>('/businesses', {
      params: { ...(status ? { status } : {}), ...(sector ? { sector } : {}) },
    }),

  sectors: () => api.get<SectorDto[]>('/businesses/sectors'),

  get: (businessId: string) =>
    api.get<BusinessDto>(`/businesses/${businessId}`),

  register: (data: RegisterBusinessRequest) =>
    api.post<{ businessId: string }>('/businesses', data),

  update: (businessId: string, data: UpdateBusinessRequest) =>
    api.patch(`/businesses/${businessId}`, data),

  approve: (businessId: string) =>
    api.post(`/businesses/${businessId}/approve`),

  reject: (businessId: string, reason: string) =>
    api.post(`/businesses/${businessId}/reject`, { reason }),

  deactivate: (businessId: string, reason: string) =>
    api.post(`/businesses/${businessId}/deactivate`, { reason }),

  activate: (businessId: string) =>
    api.post(`/businesses/${businessId}/activate`),

  close: (businessId: string) =>
    api.post(`/businesses/${businessId}/close`),

  updateCapacity: (businessId: string, data: UpdateCapacityRequest) =>
    api.put(`/businesses/${businessId}/capacity`, data),

  uploadDocument: (businessId: string, file: File, type: string) => {
    const formData = new FormData()
    formData.append('documentFile', file)
    formData.append('type', type)
    return api.post(`/businesses/${businessId}/documents`, formData)
  },

  getDocumentUrl: (businessId: string, documentId: string) =>
    api.get<{ url: string }>(`/businesses/${businessId}/documents/${documentId}/url`),

  deleteDocument: (businessId: string, documentId: string) =>
    api.delete(`/businesses/${businessId}/documents/${documentId}`),

  approveDocument: (businessId: string, documentId: string) =>
    api.post(`/businesses/${businessId}/documents/${documentId}/approve`),

  searchNearby: (latitude: number, longitude: number, radiusKm: number) =>
    api.get<BusinessDto[]>('/businesses/nearby', { params: { latitude, longitude, radiusKm } }),
}
