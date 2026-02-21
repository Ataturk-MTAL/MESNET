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
}

export interface UpdateBusinessRequest {
  name?: string
  address?: string
  phoneNumber?: string
  email?: string
  website?: string
  personnelCount?: number
  location?: GeoLocation
}

export interface UpdateCapacityRequest {
  totalSlots: number
}

export const businessApi = {
  list: (status?: string) =>
    api.get<BusinessDto[]>('/businesses', { params: status ? { status } : undefined }),

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

  updateCapacity: (businessId: string, data: UpdateCapacityRequest) =>
    api.put(`/businesses/${businessId}/capacity`, data),

  uploadDocument: (businessId: string, formData: FormData) =>
    api.post(`/businesses/${businessId}/documents`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }),

  approveDocument: (businessId: string, documentId: string) =>
    api.post(`/businesses/${businessId}/documents/${documentId}/approve`),

  searchNearby: (latitude: number, longitude: number, radiusKm: number) =>
    api.get<BusinessDto[]>('/businesses/nearby', { params: { latitude, longitude, radiusKm } }),
}
